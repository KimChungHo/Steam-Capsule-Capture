using UnityEngine;

namespace SteamCapsuleCapture.Editor
{
    // 원본 픽셀 좌표는 좌상단 원점입니다. 창 배율 및 DPI와 분리하여 계산합니다.
    internal static class CapsuleCropGeometry
    {
        public static Rect Fit(Vector2 source, float aspect, float fraction)
        {
            float width = Mathf.Min(source.x, source.y * aspect) * fraction;
            Vector2 size = new Vector2(width, width / aspect);
            return new Rect((source - size) * 0.5f, size);
        }

        public static Rect Move(Rect start, Vector2 delta, Vector2 source)
        {
            return new Rect(new Vector2(
                Mathf.Clamp(start.x + delta.x, 0, source.x - start.width),
                Mathf.Clamp(start.y + delta.y, 0, source.y - start.height)), start.size);
        }

        // 꼭짓점: 좌상단, 우상단, 우하단, 좌하단. 반대쪽 꼭짓점은 고정합니다.
        public static Rect Resize(Rect start, Vector2 pointer, int corner, Vector2 source,
            float aspect, float minimumPixels)
        {
            bool left = corner == 0 || corner == 3;
            bool top = corner == 0 || corner == 1;
            Vector2 anchor = new Vector2(left ? start.xMax : start.xMin, top ? start.yMax : start.yMin);
            float signX = left ? -1 : 1;
            float signY = top ? -1 : 1;
            Vector2 direction = new Vector2(signX * aspect, signY);
            float height = Vector2.Dot(pointer - anchor, direction) / direction.sqrMagnitude;
            float maxHeight = Mathf.Min((left ? anchor.x : source.x - anchor.x) / aspect,
                top ? anchor.y : source.y - anchor.y);
            float minHeight = Mathf.Min(maxHeight, Mathf.Max(minimumPixels, minimumPixels / aspect));
            height = Mathf.Clamp(height, minHeight, maxHeight);
            Vector2 opposite = anchor + direction * height;
            return Rect.MinMaxRect(Mathf.Min(anchor.x, opposite.x), Mathf.Min(anchor.y, opposite.y),
                Mathf.Max(anchor.x, opposite.x), Mathf.Max(anchor.y, opposite.y));
        }

        public static Rect Normalize(Rect rect, Vector2 source) => new Rect(
            rect.x / source.x, rect.y / source.y, rect.width / source.x, rect.height / source.y);

        public static Rect ToPixels(Rect normalized, Vector2 source) => new Rect(
            normalized.x * source.x, normalized.y * source.y,
            normalized.width * source.x, normalized.height * source.y);

        public static Rect Refit(Rect normalized, Vector2 source, float aspect)
        {
            Rect pixels = ToPixels(normalized, source);
            float width = Mathf.Min(pixels.width, source.x, source.y * aspect);
            Vector2 size = new Vector2(width, width / aspect);
            Rect centered = new Rect(pixels.center - size * 0.5f, size);
            return Normalize(Move(centered, Vector2.zero, source), source);
        }
    }
}
