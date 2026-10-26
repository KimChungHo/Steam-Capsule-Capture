using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SteamCapsuleCapture.Editor
{
    internal sealed class CapsuleCaptureController : IDisposable
    {
        private readonly CapsuleCaptureWindow window;
        private readonly GameViewBridge bridge = new GameViewBridge();
        private readonly CapsuleGuideView guide;
        private double nextRefresh;
        private bool disposed;
        public CapsuleCatalog Catalog { get; }
        public CapsuleCaptureModel Model => window.Model;
        public CapsulePreset Selected => Catalog.Settings.presets[Model.selectedIndex];
        public bool Ready => bridge.Ready && Model.initialized && !EditorApplication.isCompiling
            && !EditorApplication.isUpdating
            && EditorApplication.isPlaying == EditorApplication.isPlayingOrWillChangePlaymode;
        public string StatusKey => bridge.ErrorKey ?? "ready";

        public CapsuleCaptureController(CapsuleCaptureWindow window)
        {
            this.window = window;
            Catalog = new CapsuleCatalog { English = Model.english };
            Model.selectedIndex = Mathf.Clamp(Model.selectedIndex, 0, Catalog.Settings.presets.Length - 1);
            guide = new CapsuleGuideView(Catalog.Settings);
            guide.DragStarted += RecordUndo;
            guide.Dragged += Drag;
            EditorApplication.update += Update;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void Refresh()
        {
            if (disposed)
                return;
            if (bridge.Refresh())
                Model.EnsureSource(new Vector2(bridge.Texture.width, bridge.Texture.height), Selected, Catalog.Settings);
            guide.Display(bridge, Model.normalizedCrop, Model.showGuide && Model.initialized);
            window.RefreshStatus();
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup < nextRefresh)
                return;
            nextRefresh = EditorApplication.timeSinceStartup + Catalog.Settings.refreshSeconds;
            Refresh();
        }

        public void Select(int index)
        {
            RecordUndo();
            Model.selectedIndex = index;
            Model.initialized = false;
            Model.showGuide = true;
            Refresh();
            window.RefreshSelection();
        }

        public void Reset()
        {
            RecordUndo();
            Model.initialized = false;
            Refresh();
        }

        public void SetGuide(bool visible)
        {
            RecordUndo();
            Model.showGuide = visible;
            Refresh();
        }

        public void SetLanguage(bool english)
        {
            Model.english = english;
            Catalog.English = english;
            window.BuildView();
        }

        private void Drag(Rect startNormalized, Vector2 startPointer, Vector2 pointer, int corner)
        {
            Vector2 size = Model.sourceSize;
            Rect start = CapsuleCropGeometry.ToPixels(startNormalized, size);
            Rect updated = corner < 0
                ? CapsuleCropGeometry.Move(start, Vector2.Scale(pointer - startPointer, size), size)
                : CapsuleCropGeometry.Resize(start, Vector2.Scale(pointer, size), corner, size,
                    Selected.Aspect, Catalog.Settings.minimumCropPixels);
            Model.normalizedCrop = CapsuleCropGeometry.Normalize(updated, size);
            guide.Display(bridge, Model.normalizedCrop, Model.showGuide);
            window.RefreshStatus();
        }

        private void RecordUndo() => Undo.RecordObject(window, Catalog.Text("undo_crop"));

        private void OnUndoRedo()
        {
            Catalog.English = Model.english;
            Refresh();
            window.RefreshSelection();
        }

        public void Save()
        {
            try
            {
                Refresh();
                if (!Ready)
                    throw new InvalidOperationException(Catalog.Text(bridge.ErrorKey ?? "busy"));

                byte[] png = CapsulePngExporter.Capture(bridge.Texture, Model.normalizedCrop, Selected,
                    Catalog, SystemInfo.graphicsUVStartsAtTop);
                string path = CapsuleFileStore.Save(png, Selected);
                Model.lastSavedPath = path;
                window.ShowResult(string.Format(Catalog.Text("saved"), path), false);
            }
            catch (Exception exception)
            {
                window.ShowResult(string.Format(Catalog.Text("failed"), exception.Message), true);
            }
        }

        public void Reveal()
        {
            if (!string.IsNullOrEmpty(Model.lastSavedPath) && File.Exists(Model.lastSavedPath))
                EditorUtility.RevealInFinder(Model.lastSavedPath);
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            EditorApplication.update -= Update;
            Undo.undoRedoPerformed -= OnUndoRedo;
            guide.DragStarted -= RecordUndo;
            guide.Dragged -= Drag;
            guide.Dispose();
        }
    }
}
