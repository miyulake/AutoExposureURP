using UnityEngine;
using UnityEngine.Rendering;

namespace Miyu.AutoExposure.Runtime
{
    [System.Serializable, VolumeComponentMenu("Miyu/Auto Exposure")]
    public class AutoExposureSettings : VolumeComponent
    {
        [Header("Auto Exposure")]
        [Tooltip("If the current average luminance is <color=yellow>higher</color> than the target, the scene will darken.")]
        public ClampedFloatParameter targetLuminance = new(0.4f, 0.01f, 1f);
        [Tooltip("Maximum exposure compensation.\n(value of 1 corresponds to a maximum of -1 post-exposure)")]
        public ClampedFloatParameter maxDarkeningEV = new(4f, 0f, 10f);
        [Tooltip("How fast the scene darkens.")]
        public MinFloatParameter adaptationSpeed = new(3f, 0f);
        [Tooltip("1 = every frame, 2 = every other frame, etc.")]
        public ClampedIntParameter updateInterval = new(1, 1, 10);
        [Tooltip("Larger values are more accurate, but smaller values are faster.")]
        public ClampedIntParameter downsampleSize = new(32, 16, 128);
    }
}