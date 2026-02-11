using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Miyu.AutoExposure.Editor
{
    [InitializeOnLoad]
    public static class URPValidator
    {
        static URPValidator()
        {
            Validate();
            RenderPipelineManager.activeRenderPipelineTypeChanged += Validate;
        }

        private static void Validate()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Debug.LogWarning("URP is not active. Auto Exposure requires URP.");
        }
    }
}