#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class URPValidator
{
    [InitializeOnLoadMethod]
    static void Validate()
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
            Debug.LogWarning("AutoExposure requires URP. Please enable URP in Project Settings.");
    }
}
#endif