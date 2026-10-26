using System;
using UnityEngine;

namespace SteamCapsuleCapture.Editor
{
    [Serializable]
    internal sealed class CapsuleCaptureModel
    {
        public int selectedIndex;
        public bool english;
        public bool showGuide = true;
        public bool initialized;
        public Rect normalizedCrop;
        public Vector2 sourceSize;
        public string lastSavedPath;

        public void EnsureSource(Vector2 size, CapsulePreset preset, CapsuleSettings settings)
        {
            if (!initialized)
            {
                Reset(size, preset, settings);
                return;
            }
            if (size != sourceSize)
                normalizedCrop = CapsuleCropGeometry.Refit(normalizedCrop, size, preset.Aspect);
            sourceSize = size;
        }

        public void Reset(Vector2 size, CapsulePreset preset, CapsuleSettings settings)
        {
            if (size.x <= 0 || size.y <= 0)
            {
                initialized = false;
                return;
            }
            sourceSize = size;
            normalizedCrop = CapsuleCropGeometry.Normalize(
                CapsuleCropGeometry.Fit(size, preset.Aspect, settings.initialCropFraction), size);
            initialized = true;
        }
    }
}
