using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Miyu.AutoExposure.Runtime
{
    public class AutoExposure : ScriptableRendererFeature
    {
        private class LuminancePass : ScriptableRenderPass
        {
            private RTHandle m_Downsampled;
            private ComputeBuffer m_Buffer;
            private readonly ComputeShader _Compute;

            private readonly Dictionary<int, float> _CameraEV = new();
            private readonly Dictionary<int, float> _SmoothedLuminance = new();

            static readonly int SceneTexture = Shader.PropertyToID("SceneTexture");
            static readonly int LuminanceBuffer = Shader.PropertyToID("LuminanceBuffer");

            public float AverageLuminance { get; private set; }

            public LuminancePass(ComputeShader compute) => _Compute = compute;

            public void Setup() => m_Buffer ??= new ComputeBuffer(2, sizeof(uint));

            private void ApplyExposure(ref RenderingData data, AutoExposureSettings settings)
            {
                var stack = VolumeManager.instance.stack;
                var colorAdjustments = stack.GetComponent<ColorAdjustments>();
                if (colorAdjustments == null) return;

                var camID = data.cameraData.camera.GetInstanceID();

                if (!_SmoothedLuminance.TryGetValue(camID, out var smoothedLum)) smoothedLum = AverageLuminance;

                smoothedLum =
                    Mathf.Lerp(smoothedLum, AverageLuminance, settings.adaptationSpeed.value * Time.deltaTime);

                _SmoothedLuminance[camID] = smoothedLum;

                smoothedLum = Mathf.Max(smoothedLum, 0.01f);

                var deltaEV =
                    Mathf.Log(Mathf.Max(smoothedLum, 0.0001f) / settings.targetLuminance.value, 2f);
                deltaEV = Mathf.Max(0f, deltaEV);
                deltaEV = Mathf.Clamp(deltaEV, 0f, settings.maxDarkeningEV.value);

                if (!_CameraEV.TryGetValue(camID, out float currentEV)) currentEV = 0f;

                var targetEV = -deltaEV;
                currentEV = Mathf.Lerp(currentEV, targetEV, settings.adaptationSpeed.value * Time.deltaTime);

                _CameraEV[camID] = currentEV;

                colorAdjustments.postExposure.value += currentEV;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData data)
            {
                var settings = VolumeManager.instance.stack.GetComponent<AutoExposureSettings>();
                if (data.cameraData.cameraType != CameraType.Game || _Compute == null || !settings.IsActive()) return;
                if (settings.updateInterval.value > 1 && Time.frameCount % settings.updateInterval.value != 0)
                {
                    ApplyExposure(ref data, settings);
                    return;
                }

                Setup();

                var cmd = CommandBufferPool.Get("AutoExposure Luminance");

                var descriptor = data.cameraData.cameraTargetDescriptor;
                descriptor.width = settings.downsampleSize.value;
                descriptor.height = settings.downsampleSize.value;
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
                cmd.Clear();
                CommandBufferPool.Release(cmd);

                var result = new uint[2];
                m_Buffer.GetData(result);
                AverageLuminance = Mathf.Max(result[0] / Mathf.Max(result[1], 1), 0.01f);

                ApplyExposure(ref data, settings);
            }

            public void Dispose()
            {
                m_Buffer?.Release();
                m_Downsampled?.Release();
                m_Buffer = null;
                m_Downsampled = null;
                _CameraEV.Clear();
                _SmoothedLuminance.Clear();
            }
        }

        [Tooltip("When the pass is executed.")]
        [SerializeField] private RenderPassEvent m_Event = RenderPassEvent.AfterRenderingOpaques;
        [Tooltip("The compute shader used by the pass.")]
        [SerializeField] private ComputeShader m_LuminanceCompute;
        private LuminancePass m_Pass;

        public override void Create()
        {
            if (m_LuminanceCompute != null) m_Pass = new(m_LuminanceCompute);
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
}