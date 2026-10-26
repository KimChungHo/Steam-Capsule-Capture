using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SteamCapsuleCapture.Editor
{
    [Serializable]
    internal sealed class CapsulePreset
    {
        public string id;
        public int width;
        public int height;
        public string memo;
        public float Aspect => (float)width / height;
    }

    [Serializable]
    internal sealed class CapsuleSettings
    {
        public string memo;
        public float windowWidth;
        public float windowHeight;
        public float initialCropFraction;
        public float minimumCropPixels;
        public float handleSize;
        public float borderWidth;
        public double refreshSeconds;
        public float spacing;
        public float buttonHeight;
        public string guideColor;
        public string fontAssetName;
        public CapsulePreset[] presets;
    }

    internal sealed class CapsuleCatalog
    {
        [Serializable]
        private sealed class StringRow
        {
            public string key;
            public string ko;
            public string en;
        }

        [Serializable]
        private sealed class StringTable
        {
            public string memo;
            public StringRow[] entries;
        }

        private readonly Dictionary<string, StringRow> strings = new Dictionary<string, StringRow>();
        public CapsuleSettings Settings { get; }
        public bool English { get; set; }

        public CapsuleCatalog()
        {
            // 설치 폴더가 바뀌어도 동일한 어셈블리 옆의 테이블을 찾습니다.
            string root = null;
            foreach (string guid in AssetDatabase.FindAssets("SteamCapsuleCapture.Editor t:AssemblyDefinitionAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == "SteamCapsuleCapture.Editor.asmdef")
                {
                    root = Path.GetDirectoryName(path).Replace('\\', '/');
                    break;
                }
            }

            if (root == null)
                throw new FileNotFoundException("SteamCapsuleCapture.Editor.asmdef");

            Settings = JsonUtility.FromJson<CapsuleSettings>(Read(root + "/Data/SteamCapsuleSettings.json"));
            StringTable table = JsonUtility.FromJson<StringTable>(Read(root + "/Data/SteamCapsuleStrings.json"));
            if (table?.entries == null)
                throw new InvalidDataException("SteamCapsuleStrings.json");
            foreach (StringRow row in table.entries)
            {
                if (string.IsNullOrEmpty(row.key) || string.IsNullOrEmpty(row.ko) || string.IsNullOrEmpty(row.en))
                    throw new InvalidDataException("SteamCapsuleStrings.json: key/ko/en");
                strings.Add(row.key, row);
            }

            ValidateSettings();
        }

        public string Text(string key) => strings.TryGetValue(key, out StringRow row)
            ? (English ? row.en : row.ko) : key;

        public Font FindFont()
        {
            foreach (string guid in AssetDatabase.FindAssets(Settings.fontAssetName + " t:Font"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == Settings.fontAssetName)
                    return AssetDatabase.LoadAssetAtPath<Font>(path);
            }
            return null;
        }

        private static string Read(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset == null)
                throw new FileNotFoundException(path);
            return asset.text;
        }

        private void ValidateSettings()
        {
            if (Settings?.presets == null || Settings.presets.Length == 0
                || Settings.initialCropFraction <= 0 || Settings.initialCropFraction > 1
                || Settings.minimumCropPixels <= 0 || Settings.handleSize <= 0
                || Settings.borderWidth <= 0 || Settings.refreshSeconds <= 0
                || Settings.windowWidth <= 0 || Settings.windowHeight <= 0
                || Settings.spacing < 0 || Settings.buttonHeight <= 0
                || !ColorUtility.TryParseHtmlString(Settings.guideColor, out _))
                throw new InvalidDataException("SteamCapsuleSettings.json");

            var ids = new HashSet<string>();
            foreach (CapsulePreset preset in Settings.presets)
            {
                if (string.IsNullOrEmpty(preset.id) || preset.width <= 0 || preset.height <= 0
                    || !ids.Add(preset.id) || !strings.ContainsKey(preset.id))
                    throw new InvalidDataException("SteamCapsuleSettings.json: presets");
            }
        }
    }
}
