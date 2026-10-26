using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SteamCapsuleCapture.Editor
{
    // Unity 비공개 API는 이 어댑터에만 한정합니다. 다른 프로젝트 코드에 의존하지 않습니다.
    internal sealed class GameViewBridge
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type GameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        private static readonly PropertyInfo ViewRect = GameViewType?.GetProperty("viewInWindow", Members);
        private static readonly PropertyInfo TargetRect = GameViewType?.GetProperty("targetInView", Members);
        private static readonly FieldInfo RenderedTexture = GameViewType?.GetField("m_RenderTexture", Members);
        private static readonly FieldInfo Parent = typeof(EditorWindow).GetField("m_Parent", Members);

        public EditorWindow Window { get; private set; }
        public RenderTexture Texture { get; private set; }
        public Rect Viewport { get; private set; }
        public Rect ImageInWindow { get; private set; }
        public string ErrorKey { get; private set; }
        public bool Ready => Window != null && Texture != null && Texture.IsCreated();

        public bool Refresh()
        {
            Texture = null;
            ErrorKey = null;
            if (GameViewType == null || ViewRect == null || TargetRect == null || RenderedTexture == null)
                return Fail("unsupported");

            try
            {
                if (!IsVisible(Window))
                    Window = FindVisible();
                if (Window == null)
                    return Fail("no_view");

                Viewport = (Rect)ViewRect.GetValue(Window);
                Rect target = (Rect)TargetRect.GetValue(Window);
                ImageInWindow = new Rect(Viewport.position + target.position, target.size);
                Texture = RenderedTexture.GetValue(Window) as RenderTexture;
                if (Texture == null || !Texture.IsCreated() || target.width <= 0 || target.height <= 0
                    || Viewport.width <= 0 || Viewport.height <= 0)
                    return Fail("no_frame");
                return true;
            }
            catch (Exception)
            {
                return Fail("unsupported");
            }
        }

        private bool Fail(string errorKey)
        {
            ErrorKey = errorKey;
            Texture = null;
            return false;
        }

        private static bool IsVisible(EditorWindow window)
        {
            if (window == null || window.GetType() != GameViewType || window.rootVisualElement.panel == null)
                return false;
            object parent = Parent?.GetValue(window);
            PropertyInfo actualView = parent?.GetType().GetProperty("actualView", Members);
            return parent != null && (actualView == null || actualView.GetValue(parent) as EditorWindow == window);
        }

        private static EditorWindow FindVisible()
        {
            // 읽기만 수행하며 Game 탭이나 OS 창의 포커스를 변경하지 않습니다.
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(GameViewType))
                if (candidate is EditorWindow window && IsVisible(window))
                    return window;
            return null;
        }
    }
}
