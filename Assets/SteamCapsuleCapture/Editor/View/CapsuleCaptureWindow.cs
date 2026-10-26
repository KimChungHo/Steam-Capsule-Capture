using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SteamCapsuleCapture.Editor
{
    public sealed class CapsuleCaptureWindow : EditorWindow
    {
        [SerializeField] private CapsuleCaptureModel model = new CapsuleCaptureModel();
        private CapsuleCaptureController controller;
        private Button[] presetButtons;
        private Button saveButton;
        private Button revealButton;
        private Toggle guideToggle;
        private Label outputLabel;
        private Label sourceLabel;
        private HelpBox statusBox;
        private HelpBox upscaleBox;
        private HelpBox heroBox;
        private HelpBox resultBox;
        internal CapsuleCaptureModel Model => model;

        // MenuItem은 컴파일 상수만 지원합니다. 문자열 테이블의 menu 한국어 값과 일치시킵니다.
        [MenuItem("도구/Steam Capsule Capture")]
        public static void Open() => GetWindow<CapsuleCaptureWindow>();

        private void OnEnable()
        {
            model ??= new CapsuleCaptureModel();
        }

        public void CreateGUI()
        {
            try
            {
                controller?.Dispose();
                controller = new CapsuleCaptureController(this);
                BuildView();
                controller.Refresh();
            }
            catch (Exception exception)
            {
                controller?.Dispose();
                controller = null;
                rootVisualElement.Clear();
                rootVisualElement.Add(new HelpBox(exception.Message, HelpBoxMessageType.Error));
            }
        }

        internal void BuildView()
        {
            CapsuleCatalog catalog = controller.Catalog;
            CapsuleSettings settings = catalog.Settings;
            titleContent = new GUIContent(catalog.Text("title"));
            minSize = new Vector2(settings.windowWidth, settings.windowHeight);
            rootVisualElement.Clear();
            Font font = catalog.FindFont();
            if (font != null)
                rootVisualElement.style.unityFontDefinition = FontDefinition.FromFont(font);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            scroll.style.paddingLeft = settings.spacing;
            scroll.style.paddingRight = settings.spacing;
            scroll.style.paddingTop = settings.spacing;
            scroll.style.paddingBottom = settings.spacing;
            rootVisualElement.Add(scroll);

            var language = new Toggle(catalog.Text("language")) { value = model.english };
            language.RegisterValueChangedCallback(evt => controller.SetLanguage(evt.newValue));
            scroll.Add(language);
            scroll.Add(new HelpBox(catalog.Text("instructions"), HelpBoxMessageType.Info));

            var presets = new VisualElement();
            presets.style.flexDirection = FlexDirection.Column;
            presets.style.marginTop = settings.spacing;
            presets.style.marginBottom = settings.spacing;
            CapsuleGuideView.SetBorder(presets, Color.gray, settings.borderWidth * 0.5f);
            scroll.Add(presets);
            presetButtons = new Button[settings.presets.Length];
            for (int i = 0; i < settings.presets.Length; i++)
            {
                int index = i;
                CapsulePreset preset = settings.presets[i];
                Button button = MakeButton(() => controller.Select(index),
                    $"{catalog.Text(preset.id)}    {preset.width} x {preset.height}");
                presetButtons[i] = button;
                presets.Add(button);
            }

            outputLabel = new Label();
            sourceLabel = new Label();
            scroll.Add(outputLabel);
            scroll.Add(sourceLabel);
            guideToggle = new Toggle(catalog.Text("guide")) { value = model.showGuide };
            guideToggle.RegisterValueChangedCallback(evt => controller.SetGuide(evt.newValue));
            scroll.Add(guideToggle);
            scroll.Add(MakeButton(controller.Reset, catalog.Text("reset")));
            saveButton = MakeButton(controller.Save, catalog.Text("save"));
            scroll.Add(saveButton);
            revealButton = MakeButton(controller.Reveal, catalog.Text("reveal"));
            scroll.Add(revealButton);

            heroBox = new HelpBox(catalog.Text("hero_hint"), HelpBoxMessageType.Info);
            upscaleBox = new HelpBox(catalog.Text("upscale"), HelpBoxMessageType.Warning);
            statusBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            resultBox = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            resultBox.style.display = DisplayStyle.None;
            scroll.Add(heroBox);
            scroll.Add(upscaleBox);
            scroll.Add(statusBox);
            scroll.Add(resultBox);
            RefreshSelection();
            RefreshStatus();
        }

        private Button MakeButton(Action action, string text)
        {
            var button = new Button(action) { text = text };
            button.style.minHeight = controller.Catalog.Settings.buttonHeight;
            return button;
        }

        internal void RefreshSelection()
        {
            if (controller == null || presetButtons == null)
                return;
            ColorUtility.TryParseHtmlString(controller.Catalog.Settings.guideColor, out Color selectedColor);
            for (int i = 0; i < presetButtons.Length; i++)
                CapsuleGuideView.SetBorder(presetButtons[i], i == model.selectedIndex ? selectedColor : Color.gray,
                    controller.Catalog.Settings.borderWidth);
            guideToggle.SetValueWithoutNotify(model.showGuide);
        }

        internal void RefreshStatus()
        {
            if (controller == null || statusBox == null)
                return;
            CapsuleCatalog catalog = controller.Catalog;
            CapsulePreset selected = controller.Selected;
            Rect pixels = CapsuleCropGeometry.ToPixels(model.normalizedCrop, model.sourceSize);
            outputLabel.text = string.Format(catalog.Text("output"), selected.width, selected.height);
            sourceLabel.text = string.Format(catalog.Text("source"), Mathf.RoundToInt(pixels.width), Mathf.RoundToInt(pixels.height));
            saveButton.SetEnabled(controller.Ready);
            revealButton.SetEnabled(!string.IsNullOrEmpty(model.lastSavedPath) && File.Exists(model.lastSavedPath));
            statusBox.text = catalog.Text(controller.StatusKey);
            statusBox.messageType = controller.Ready ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
            upscaleBox.style.display = controller.Ready && (pixels.width < selected.width || pixels.height < selected.height)
                ? DisplayStyle.Flex : DisplayStyle.None;
            heroBox.style.display = selected.id == "library_hero" ? DisplayStyle.Flex : DisplayStyle.None;
        }

        internal void ShowResult(string text, bool error)
        {
            resultBox.text = text;
            resultBox.messageType = error ? HelpBoxMessageType.Error : HelpBoxMessageType.Info;
            resultBox.style.display = DisplayStyle.Flex;
            RefreshStatus();
        }

        private void OnDisable()
        {
            controller?.Dispose();
            controller = null;
        }
    }
}
