using System;
using UnityEngine;

namespace SteamCapsuleCapture.Editor
{
    internal static class CapsulePngExporter
    {
        public static byte[] Capture(RenderTexture source, Rect normalized, CapsulePreset preset,
            CapsuleCatalog catalog, bool flipVertically)
        {
            if (source == null || !source.IsCreated() || normalized.width <= 0 || normalized.height <= 0
                || normalized.xMin < 0 || normalized.yMin < 0 || normalized.xMax > 1.00001f
                || normalized.yMax > 1.00001f || preset.width <= 0 || preset.height <= 0)
                throw new InvalidOperationException(catalog.Text("invalid_crop"));

            // 선택 영역은 좌상단 원점입니다. Game 뷰가 화면에 그릴 때 적용하는 상하 반전을 동일하게 적용합니다.
            int x = Mathf.Clamp(Mathf.FloorToInt(normalized.x * source.width), 0, source.width - 1);
            int top = Mathf.Clamp(Mathf.FloorToInt(normalized.y * source.height), 0, source.height - 1);
            int width = Mathf.Clamp(Mathf.CeilToInt(normalized.xMax * source.width) - x, 1, source.width - x);
            int height = Mathf.Clamp(Mathf.CeilToInt(normalized.yMax * source.height) - top, 1, source.height - top);
            int y = flipVertically ? top : source.height - top - height;

            RenderTexture previous = RenderTexture.active;
            Texture2D crop = null;
            Texture2D output = null;
            try
            {
                // 에디터 가이드는 Game 뷰의 UI에만 있으므로 원본 렌더 텍스처에 포함되지 않습니다.
                RenderTexture.active = source;
                crop = new Texture2D(width, height, TextureFormat.RGB24, false);
                crop.ReadPixels(new Rect(x, y, width, height), 0, 0, false);
                Color32[] pixels = crop.GetPixels32();
                Color32[] resized = Resize(pixels, width, height, preset.width, preset.height, flipVertically);
                output = new Texture2D(preset.width, preset.height, TextureFormat.RGB24, false);
                output.SetPixels32(resized);
                byte[] png = output.EncodeToPNG();
                if (png == null || png.Length == 0)
                    throw new InvalidOperationException(catalog.Text("encode_failed"));
                return png;
            }
            finally
            {
                RenderTexture.active = previous;
                if (crop != null)
                    UnityEngine.Object.DestroyImmediate(crop);
                if (output != null)
                    UnityEngine.Object.DestroyImmediate(output);
            }
        }

        // GPU 색 공간 설정을 변경하지 않고 캡처된 색상 그대로 보간합니다.
        internal static Color32[] Resize(Color32[] source, int width, int height,
            int outputWidth, int outputHeight, bool flip)
        {
            var result = new Color32[checked(outputWidth * outputHeight)];
            for (int y = 0; y < outputHeight; y++)
            {
                float sy = Mathf.Clamp((y + 0.5f) * height / outputHeight - 0.5f, 0, height - 1);
                if (flip)
                    sy = height - 1 - sy;
                int y0 = Mathf.FloorToInt(sy);
                int y1 = Mathf.Min(y0 + 1, height - 1);
                float fy = sy - y0;
                for (int x = 0; x < outputWidth; x++)
                {
                    float sx = Mathf.Clamp((x + 0.5f) * width / outputWidth - 0.5f, 0, width - 1);
                    int x0 = Mathf.FloorToInt(sx);
                    int x1 = Mathf.Min(x0 + 1, width - 1);
                    float fx = sx - x0;
                    Color32 lower = Color32.Lerp(source[y0 * width + x0], source[y0 * width + x1], fx);
                    Color32 upper = Color32.Lerp(source[y1 * width + x0], source[y1 * width + x1], fx);
                    result[y * outputWidth + x] = Color32.Lerp(lower, upper, fy);
                }
            }
            return result;
        }
    }
}
