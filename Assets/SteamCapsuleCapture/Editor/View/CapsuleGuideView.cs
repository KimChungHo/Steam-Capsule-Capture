using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SteamCapsuleCapture.Editor
{
    internal sealed class CapsuleGuideView : IDisposable
    {
        private readonly VisualElement root = new VisualElement();
        private readonly VisualElement frame = new VisualElement();
        private readonly VisualElement[] handles = new VisualElement[4];
        private readonly CapsuleSettings settings;
        private EditorWindow host;
        private Rect imageInWindow;
        private Rect viewport;
        private int activePointer = -1;
        private int activeCorner;
        private Vector2 dragStart;
        private Rect cropAtStart;
        private Rect crop;
        private VisualElement captureTarget;

        public event Action DragStarted;
        public event Action<Rect, Vector2, Vector2, int> Dragged;
        public bool IsDragging => activePointer >= 0;

        public CapsuleGuideView(CapsuleSettings settings)
        {
            this.settings = settings;
            ColorUtility.TryParseHtmlString(settings.guideColor, out Color color);
            root.name = "steam-capsule-guide";
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.overflow = Overflow.Hidden;
            frame.name = "steam-capsule-crop";
            frame.style.position = Position.Absolute;
            SetBorder(frame, color, settings.borderWidth);
            root.Add(frame);
            RegisterDrag(frame, -1);

            for (int i = 0; i < handles.Length; i++)
            {
                var handle = new VisualElement { name = "steam-capsule-corner-" + i };
                handle.style.position = Position.Absolute;
                handle.style.width = settings.handleSize;
                handle.style.height = settings.handleSize;
                handle.style.backgroundColor = color;
                SetBorder(handle, Color.white, settings.borderWidth * 0.5f);
                handles[i] = handle;
                root.Add(handle);
                RegisterDrag(handle, i);
            }
        }

        public void Display(GameViewBridge bridge, Rect normalized, bool visible)
        {
            if (!visible || !bridge.Ready)
            {
                Detach();
                return;
            }
            if (host != bridge.Window || root.parent != bridge.Window.rootVisualElement)
            {
                Detach();
                host = bridge.Window;
                host.rootVisualElement.Add(root);
            }

            // Game 뷰 크기/배율 변경 중의 드래그는 취소하여 좌표계가 섞이지 않게 합니다.
            if (imageInWindow != bridge.ImageInWindow || viewport != bridge.Viewport)
                CancelDrag();
            imageInWindow = bridge.ImageInWindow;
            viewport = bridge.Viewport;
            crop = normalized;
            SetRect(root, viewport);
            Rect guide = new Rect(imageInWindow.position - viewport.position
                + Vector2.Scale(normalized.position, imageInWindow.size),
                Vector2.Scale(normalized.size, imageInWindow.size));
            SetRect(frame, guide);
            for (int i = 0; i < handles.Length; i++)
            {
                float x = i == 0 || i == 3 ? guide.xMin : guide.xMax;
                float y = i == 0 || i == 1 ? guide.yMin : guide.yMax;
                handles[i].style.left = x - settings.handleSize * 0.5f;
                handles[i].style.top = y - settings.handleSize * 0.5f;
            }
            root.BringToFront();
        }

        private void RegisterDrag(VisualElement target, int corner)
        {
            target.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || IsDragging)
                    return;
                DragStarted?.Invoke();
                activePointer = evt.pointerId;
                activeCorner = corner;
                captureTarget = target;
                cropAtStart = crop;
                dragStart = ToNormalized(evt.position);
                target.CapturePointer(activePointer);
                evt.StopImmediatePropagation();
            });
            target.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (evt.pointerId != activePointer || captureTarget != target)
                    return;
                Dragged?.Invoke(cropAtStart, dragStart, ToNormalized(evt.position), activeCorner);
                evt.StopImmediatePropagation();
            });
            target.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.pointerId != activePointer || captureTarget != target)
                    return;
                CancelDrag();
                evt.StopImmediatePropagation();
            });
            target.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (captureTarget == target)
                {
                    activePointer = -1;
                    captureTarget = null;
                }
            });
        }

        private Vector2 ToNormalized(Vector3 panelPosition)
        {
            Vector2 point = host.rootVisualElement.WorldToLocal(panelPosition);
            return new Vector2((point.x - imageInWindow.x) / imageInWindow.width,
                (point.y - imageInWindow.y) / imageInWindow.height);
        }

        private void CancelDrag()
        {
            int pointer = activePointer;
            VisualElement target = captureTarget;
            activePointer = -1;
            captureTarget = null;
            if (pointer >= 0 && target != null && target.HasPointerCapture(pointer))
                target.ReleasePointer(pointer);
        }

        private void Detach()
        {
            CancelDrag();
            root.RemoveFromHierarchy();
            host = null;
        }

        public void Dispose() => Detach();

        private static void SetRect(VisualElement element, Rect rect)
        {
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = rect.width;
            element.style.height = rect.height;
        }

        internal static void SetBorder(VisualElement element, Color color, float width)
        {
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
        }
    }
}
