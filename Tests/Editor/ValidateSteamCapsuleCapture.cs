using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class ValidateSteamCapsuleCapture
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static Assembly assembly;
    private static int checks;

    public static string BeginGuideInteractions()
    {
        Assembly toolAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .Single(item => item.GetName().Name == "SteamCapsuleCapture.Editor");
        var windowType = toolAssembly.GetType("SteamCapsuleCapture.Editor.CapsuleCaptureWindow", true);
        if (Resources.FindObjectsOfTypeAll<EditorWindow>().Any(item => item.name == "SteamCapsuleValidationHiddenWindow"))
            throw new InvalidOperationException("A guide validation window already exists.");
        var window = (EditorWindow)ScriptableObject.CreateInstance(windowType);
        window.name = "SteamCapsuleValidationHiddenWindow";
        windowType.GetMethod("CreateGUI").Invoke(window, null);
        return "Hidden validation window created. Run RunGuideInteractions after the next editor layout.";
    }

    public static string RunGuideInteractions()
    {
        checks = 0;
        EditorWindow window = Resources.FindObjectsOfTypeAll<EditorWindow>()
            .Single(item => item.name == "SteamCapsuleValidationHiddenWindow");
        var savedFiles = new System.Collections.Generic.List<string>();
        try
        {
            object controller = window.GetType().GetField("controller", Members).GetValue(window);
            object model = window.GetType().GetField("model", Members).GetValue(window);
            var game = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .First(item => item.GetType().FullName == "UnityEditor.GameView");
            var guide = (VisualElement)Get(Get(controller, "guide"), "root");
            VisualElement frame = guide.Q("steam-capsule-crop");
            Assert(frame != null && frame.worldBound.width > 0, "live guide layout");
            Assert(frame.ContainsPoint(frame.layout.size * 0.5f), "interior is a move target");
            foreach (Vector2 fraction in new[] { new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.25f),
                new Vector2(0.25f, 0.75f), new Vector2(0.75f, 0.75f) })
                Assert(guide.panel.Pick(frame.LocalToWorld(Vector2.Scale(frame.layout.size, fraction))) == frame,
                    "interior quadrant receives pointer picking");
            Assert(guide.panel.Pick(frame.worldBound.center) == frame, "center receives pointer picking");
            Rect original = (Rect)Get(model, "normalizedCrop");
            Vector2 from = frame.worldBound.center;
            SendPointer(frame, EventType.MouseDown, from);
            SendPointer(frame, EventType.MouseMove, from + new Vector2(50, 30));
            SendPointer(frame, EventType.MouseUp, from + new Vector2(50, 30));
            Rect moved = (Rect)Get(model, "normalizedCrop");
            Assert(moved.x > original.x && moved.y > original.y, "pointer moves crop");
            Assert(moved.size == original.size, "pointer move preserves size");

            VisualElement handle = guide.Q("steam-capsule-corner-2");
            Assert(guide.panel.Pick(handle.worldBound.center) == handle, "corner takes priority over move area");
            from = handle.worldBound.center;
            SendPointer(handle, EventType.MouseDown, from);
            SendPointer(handle, EventType.MouseMove, from - new Vector2(100, 50));
            SendPointer(handle, EventType.MouseUp, from - new Vector2(100, 50));
            Rect resized = (Rect)Get(model, "normalizedCrop");
            Assert(resized.width < moved.width && resized.height < moved.height, "pointer resizes crop");
            Vector2 source = (Vector2)Get(model, "sourceSize");
            Assert(Mathf.Abs(resized.width * source.x / (resized.height * source.y) - 920f / 430) < 0.001f,
                "pointer resize preserves preset aspect");

            for (int i = 0; i < 2; i++)
            {
                Invoke(controller, "Save");
                string path = (string)Get(model, "lastSavedPath");
                Assert(!string.IsNullOrEmpty(path) && System.IO.File.Exists(path), "save writes PNG without dialog");
                Assert(!savedFiles.Contains(path), "repeated save uses a unique path");
                savedFiles.Add(path);
                string expected = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Screenshots");
                Assert(System.IO.Path.GetDirectoryName(path) == expected, "save uses project root Screenshots folder");
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                Assert(width == 920 && height == 430, "saved PNG retains preset resolution");
            }

            Invoke(controller, "Select", 7);
            Assert((int)Get(model, "selectedIndex") == 7, "preset selection");
            Rect hero = (Rect)Get(model, "normalizedCrop");
            Assert(Mathf.Abs(hero.width * source.x / (hero.height * source.y) - 3840f / 1240) < 0.001f,
                "hero preset aspect");
            Invoke(controller, "SetGuide", false);
            Assert(guide.parent == null, "toggle removes guide");
            Invoke(controller, "SetGuide", true);
            Assert(guide.parent == game.rootVisualElement, "toggle restores guide");
            Invoke(controller, "SetLanguage", true);
            Assert(window.titleContent.text == "Steam Capsule Capture", "English UI");
            Assert(window.rootVisualElement.Query<Button>().ToList().Count == 11, "8 presets and 3 actions");

            Undo.FlushUndoRecordObjects();
            Undo.ClearUndo(window);
            UnityEngine.Object.DestroyImmediate(window);
            Assert(guide.parent == null, "window disposal removes guide");
            return "SteamCapsuleCapture live guide: " + checks + " checks passed. Window was never shown or focused.";
        }
        finally
        {
            foreach (string path in savedFiles)
                System.IO.File.Delete(path);
            if (window != null)
            {
                Undo.ClearUndo(window);
                UnityEngine.Object.DestroyImmediate(window);
            }
        }
    }

    private static void SendPointer(VisualElement target, EventType type, Vector2 position)
    {
        var input = new Event { type = type, button = 0, mousePosition = position };
        if (type == EventType.MouseDown)
        {
            using (var evt = PointerDownEvent.GetPooled(input)) target.SendEvent(evt);
        }
        else if (type == EventType.MouseUp)
        {
            using (var evt = PointerUpEvent.GetPooled(input)) target.SendEvent(evt);
        }
        else
        {
            using (var evt = PointerMoveEvent.GetPooled(input)) target.SendEvent(evt);
        }
    }

    public static string RunGeometryAndPixels()
    {
        checks = 0;
        assembly = AppDomain.CurrentDomain.GetAssemblies().Single(item => item.GetName().Name == "SteamCapsuleCapture.Editor");
        float[] aspects = { 920f / 430, 462f / 174, 1232f / 706, 748f / 896,
            1438f / 810, 600f / 900, 920f / 430, 3840f / 1240 };
        foreach (float aspect in aspects)
            CheckGeometry(aspect);
        CheckResizePixels();
        return "SteamCapsuleCapture geometry and pixels: " + checks + " checks passed.";
    }

    public static string Run()
    {
        checks = 0;
        assembly = AppDomain.CurrentDomain.GetAssemblies().Single(item => item.GetName().Name == "SteamCapsuleCapture.Editor");
        object catalog = Activator.CreateInstance(Type("CapsuleCatalog"), true);
        object settings = Get(catalog, "Settings");
        Array presets = (Array)Get(settings, "presets");
        Assert(presets.Length == 8, "8 presets");
        int[,] sizes = { { 920, 430 }, { 462, 174 }, { 1232, 706 }, { 748, 896 },
            { 1438, 810 }, { 600, 900 }, { 920, 430 }, { 3840, 1240 } };

        for (int i = 0; i < presets.Length; i++)
        {
            object preset = presets.GetValue(i);
            int width = (int)Get(preset, "width");
            int height = (int)Get(preset, "height");
            Assert(width == sizes[i, 0] && height == sizes[i, 1], "preset resolution " + i);
            string id = (string)Get(preset, "id");
            Assert((string)Invoke(catalog, "Text", id) != id, "Korean text " + id);
            Type("CapsuleCatalog").GetProperty("English").SetValue(catalog, true);
            Assert((string)Invoke(catalog, "Text", id) != id, "English text " + id);
            Type("CapsuleCatalog").GetProperty("English").SetValue(catalog, false);
            CheckGeometry((float)width / height);
        }
        CheckResizePixels();
        CheckPngOutput(catalog, presets);
        CheckCropOrientation(catalog);

        var gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        Assert(gameView?.GetProperty("viewInWindow", Members) != null, "GameView viewport API");
        Assert(gameView?.GetProperty("targetInView", Members) != null, "GameView image API");
        Assert(gameView?.GetField("m_RenderTexture", Members) != null, "GameView texture API");
        return "SteamCapsuleCapture: " + checks + " checks passed. No scene or focus changes.";
    }

    private static void CheckGeometry(float aspect)
    {
        var source = new Vector2(1920, 1080);
        Rect start = (Rect)Static("CapsuleCropGeometry", "Fit", source, aspect, 0.8f);
        CheckBounds(start, source, aspect);
        Rect normalized = (Rect)Static("CapsuleCropGeometry", "Normalize", start, source);
        Rect restored = (Rect)Static("CapsuleCropGeometry", "ToPixels", normalized, source);
        Assert(Vector2.Distance(start.position, restored.position) < 0.001f, "normalize position roundtrip");
        Assert(Vector2.Distance(start.size, restored.size) < 0.001f, "normalize size roundtrip");

        for (int corner = 0; corner < 4; corner++)
        {
            bool left = corner == 0 || corner == 3;
            bool top = corner == 0 || corner == 1;
            Vector2 anchor = new Vector2(left ? start.xMax : start.xMin, top ? start.yMax : start.yMin);
            Vector2[] pointers = { new Vector2(-5000, -5000), new Vector2(5000, 5000),
                new Vector2(-5000, 5000), new Vector2(5000, -5000), anchor, source * 0.5f };
            foreach (Vector2 pointer in pointers)
            {
                Rect resized = (Rect)Static("CapsuleCropGeometry", "Resize", start, pointer, corner, source, aspect, 16f);
                CheckBounds(resized, source, aspect);
                Vector2 fixedCorner = new Vector2(left ? resized.xMax : resized.xMin, top ? resized.yMax : resized.yMin);
                Assert(Vector2.Distance(anchor, fixedCorner) < 0.001f, "opposite corner stays fixed");
            }
        }

        foreach (Vector2 delta in new[] { new Vector2(-5000, -5000), new Vector2(5000, 5000) })
        {
            Rect moved = (Rect)Static("CapsuleCropGeometry", "Move", start, delta, source);
            CheckBounds(moved, source, aspect);
            Assert(moved.size == start.size, "move preserves size");
        }

        Vector2 portraitSource = new Vector2(800, 1200);
        Rect refit = (Rect)Static("CapsuleCropGeometry", "Refit", normalized, portraitSource, aspect);
        Rect refitPixels = (Rect)Static("CapsuleCropGeometry", "ToPixels", refit, portraitSource);
        CheckBounds(refitPixels, portraitSource, aspect);
    }

    private static void CheckBounds(Rect rect, Vector2 bounds, float aspect)
    {
        Assert(rect.width > 0 && rect.height > 0, "positive crop");
        Assert(rect.xMin >= -0.001f && rect.yMin >= -0.001f
            && rect.xMax <= bounds.x + 0.001f && rect.yMax <= bounds.y + 0.001f, "crop inside image");
        Assert(Mathf.Abs(rect.width / rect.height - aspect) < 0.001f, "fixed aspect ratio");
    }

    private static void CheckResizePixels()
    {
        var pixels = new[] { new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255),
            new Color32(0, 0, 255, 255), new Color32(255, 255, 255, 255) };
        var identity = (Color32[])Static("CapsulePngExporter", "Resize", pixels, 2, 2, 2, 2, false);
        var flipped = (Color32[])Static("CapsulePngExporter", "Resize", pixels, 2, 2, 2, 2, true);
        for (int i = 0; i < pixels.Length; i++)
        {
            Assert(identity[i].Equals(pixels[i]), "identity color " + i);
            Assert(flipped[i].Equals(pixels[(1 - i / 2) * 2 + i % 2]), "vertical flip " + i);
        }
        var enlarged = (Color32[])Static("CapsulePngExporter", "Resize", pixels, 2, 2, 9, 7, false);
        Assert(enlarged.Length == 63, "fixed resize dimensions");
        Assert(enlarged[0].Equals(pixels[0]) && enlarged[62].Equals(pixels[3]), "resize corner colors");
    }

    private static void CheckPngOutput(object catalog, Array presets)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture source = RenderTexture.GetTemporary(32, 16, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Texture2D decoded = null;
        try
        {
            RenderTexture.active = source;
            GL.Clear(true, true, Color.green);
            decoded = new Texture2D(2, 2, TextureFormat.RGB24, false);
            foreach (object preset in presets)
            {
                foreach (Rect crop in new[] { new Rect(0, 0, 1, 1), new Rect(0.25f, 0.25f, 0.5f, 0.5f) })
                {
                    byte[] png = (byte[])Static("CapsulePngExporter", "Capture", source, crop, preset, catalog, false);
                    Assert(ImageConversion.LoadImage(decoded, png), "PNG decodes");
                    Assert(decoded.width == (int)Get(preset, "width") && decoded.height == (int)Get(preset, "height"),
                        "PNG has preset dimensions at both crop scales");
                    Color center = decoded.GetPixel(decoded.width / 2, decoded.height / 2);
                    Assert(center.g > 0.99f && center.r < 0.01f && center.b < 0.01f, "PNG preserves color");
                    Assert(RenderTexture.active == source, "active render texture restored");
                }
            }
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(source);
            if (decoded != null)
                UnityEngine.Object.DestroyImmediate(decoded);
        }
    }

    private static void CheckCropOrientation(object catalog)
    {
        Texture2D input = null;
        Texture2D reference = null;
        Texture2D decoded = null;
        RenderTexture previous = RenderTexture.active;
        RenderTexture source = RenderTexture.GetTemporary(32, 16, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        try
        {
            input = new Texture2D(32, 16, TextureFormat.RGB24, false);
            var pixels = new Color32[32 * 16];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 32; x++)
                    pixels[y * 32 + x] = y < 8 ? (x < 16 ? Color.red : Color.green)
                        : (x < 16 ? Color.blue : Color.white);
            input.SetPixels32(pixels);
            input.Apply();
            Graphics.Blit(input, source);
            RenderTexture.active = source;
            reference = new Texture2D(32, 16, TextureFormat.RGB24, false);
            reference.ReadPixels(new Rect(0, 0, 32, 16), 0, 0, false);
            decoded = new Texture2D(2, 2, TextureFormat.RGB24, false);
            object preset = Activator.CreateInstance(Type("CapsulePreset"), true);
            preset.GetType().GetField("width").SetValue(preset, 16);
            preset.GetType().GetField("height").SetValue(preset, 8);

            foreach (bool flip in new[] { false, true })
            {
                byte[] png = (byte[])Static("CapsulePngExporter", "Capture", source, new Rect(0, 0, 1, 1), preset, catalog, flip);
                Assert(ImageConversion.LoadImage(decoded, png), "oriented PNG decodes");
                Assert(SameRgb(decoded.GetPixel(2, 1), reference.GetPixel(4, flip ? 13 : 2)), "PNG bottom row orientation");
                Assert(SameRgb(decoded.GetPixel(13, 6), reference.GetPixel(27, flip ? 2 : 13)), "PNG top row orientation");
                png = (byte[])Static("CapsulePngExporter", "Capture", source, new Rect(0, 0, 0.5f, 0.5f), preset, catalog, flip);
                Assert(ImageConversion.LoadImage(decoded, png), "top-left crop PNG decodes");
                Assert(SameRgb(decoded.GetPixel(8, 4), reference.GetPixel(8, flip ? 4 : 12)), "top-left crop selects correct source quadrant");
            }
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(source);
            if (input != null) UnityEngine.Object.DestroyImmediate(input);
            if (reference != null) UnityEngine.Object.DestroyImmediate(reference);
            if (decoded != null) UnityEngine.Object.DestroyImmediate(decoded);
        }
    }

    private static bool SameRgb(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.01f
        && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;

    private static Type Type(string name) => assembly.GetType("SteamCapsuleCapture.Editor." + name, true);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Members)?.GetValue(target)
        ?? target.GetType().GetField(name, Members).GetValue(target);
    private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Members).Invoke(target, args);
    private static object Static(string name, string method, params object[] args) => Type(name).GetMethod(method, Members).Invoke(null, args);
    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        checks++;
    }
}
