using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AutoExposure : ScriptableRendererFeature
{
    private class LuminancePass : ScriptableRenderPass
    {
        private RTHandle m_Downsampled;
        private ComputeBuffer m_Buffer;
        private readonly ComputeShader _Compute;

        private readonly float _TargetLuminance;
        private readonly float _MaxDarkeningEV;
        private readonly float _AdaptationSpeed;
        private readonly int _DownsampleSize;

        private readonly Dictionary<int, float> _CameraEV = new();

        static readonly int SceneTexture = Shader.PropertyToID("SceneTexture");
        static readonly int LuminanceBuffer = Shader.PropertyToID("LuminanceBuffer");

        public float AverageLuminance { get; private set; }

        public LuminancePass(ComputeShader compute, float targetLuminance, float maxDarkeningEV, float adaptationSpeed, int downsampleSize)
        {
            _Compute = compute;
            _TargetLuminance = targetLuminance;
            _MaxDarkeningEV = maxDarkeningEV;
            _AdaptationSpeed = adaptationSpeed;
            _DownsampleSize = downsampleSize;
        }

        public void Setup() => m_Buffer ??= new ComputeBuffer(2, sizeof(uint));

        void ApplyExposure(ref RenderingData data)
        {
            var stack = VolumeManager.instance.stack;
            var colorAdjustments = stack.GetComponent<ColorAdjustments>();
            if (colorAdjustments == null) return;

            var luminance = AverageLuminance;
            var deltaEV = Mathf.Log(luminance / _TargetLuminance, 2f);
            deltaEV = Mathf.Max(0f, deltaEV);
            deltaEV = Mathf.Clamp(deltaEV, 0f, _MaxDarkeningEV);

            var camID = data.cameraData.camera.GetInstanceID();
            if (!_CameraEV.TryGetValue(camID, out float currentEV)) currentEV = 0f;

            var targetEV = -deltaEV;
            currentEV = Mathf.Lerp(currentEV, targetEV, _AdaptationSpeed * Time.deltaTime);

            _CameraEV[camID] = currentEV;

            var baseExposure = colorAdjustments.postExposure.value;
            colorAdjustments.postExposure.value = baseExposure + currentEV;

        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData data)
        {
            if (data.cameraData.cameraType != CameraType.Game || _Compute == null) return;

            Setup();

            var cmd = CommandBufferPool.Get("AutoExposure Luminance");

            var descriptor = data.cameraData.cameraTargetDescriptor;
            descriptor.width = _DownsampleSize;
            descriptor.height = _DownsampleSize;
            descriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(
                ref m_Downsampled,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_AutoExposureDownsample"
            );

            cmd.SetBufferData(m_Buffer, new uint[2]);

            Blitter.BlitCameraTexture(
                cmd,
                data.cameraData.renderer.cameraColorTargetHandle,
                m_Downsampled
            );

            var kernel = _Compute.FindKernel("CSMain");
            _Compute.SetTexture(kernel, SceneTexture, m_Downsampled);
            _Compute.SetBuffer(kernel, LuminanceBuffer, m_Buffer);

            _Compute.Dispatch(
                kernel,
                Mathf.CeilToInt(m_Downsampled.rt.width / 16f),
                Mathf.CeilToInt(m_Downsampled.rt.height / 16f),
                1
            );

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            var result = new uint[2];
            m_Buffer.GetData(result);
            AverageLuminance = Mathf.Max(result[0] / Mathf.Max(result[1], 1), 0.01f);

            ApplyExposure(ref data);
        }

        public void Dispose()
        {
            m_Buffer?.Release();
            m_Buffer = null;
            m_Downsampled?.Release();
            m_Downsampled = null;
        }
    }

    [Header("Setup")]
    [SerializeField] private RenderPassEvent m_Event = RenderPassEvent.AfterRenderingTransparents;
    [SerializeField] private ComputeShader m_LuminanceCompute;

    [Header("Settings")]
    [Tooltip("If the current average luminance is <color=yellow>higher</color> than the target, the scene will darken.")]
    [SerializeField, Range(0.01f, 1)] private float m_TargetLuminance = 0.4f;
    [Tooltip("Maximum exposure compensation.\n(value of 1 corresponds to a maximum of -1 post-exposure)")]
    [SerializeField, Range(0, 10)] private float m_MaxDarkeningEV = 4f;
    [Tooltip("How fast the scene darkens.")]
    [SerializeField, Min(0)] private float m_AdaptationSpeed = 3f;
    [Tooltip("Larger values are more accurate, but smaller values are faster.")]
    [SerializeField, Range(16, 128)] private int m_DownsampleSize = 32;

    private LuminancePass m_Pass;

    public override void Create()
    {
        if (m_LuminanceCompute != null) m_Pass = new LuminancePass(m_LuminanceCompute, m_TargetLuminance, m_MaxDarkeningEV, m_AdaptationSpeed, m_DownsampleSize);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
    {
        if (m_Pass == null) return;
        m_Pass.renderPassEvent = m_Event;
        renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing)
    {
        m_Pass?.Dispose();
        m_Pass = null;
    }
}
