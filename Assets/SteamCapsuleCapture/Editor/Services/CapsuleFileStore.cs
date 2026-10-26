using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SteamCapsuleCapture.Editor
{
    internal static class CapsuleFileStore
    {
        public static string OutputDirectory => Path.Combine(
            Directory.GetParent(Application.dataPath).FullName, "Screenshots");

        public static string Save(byte[] png, CapsulePreset preset)
        {
            Directory.CreateDirectory(OutputDirectory);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            string filename = $"{preset.id}_{preset.width}x{preset.height}_{timestamp}_{Guid.NewGuid():N}.png";
            string path = Path.Combine(OutputDirectory, filename);

            // 기존 파일은 덮어쓰지 않고 이번 쓰기에 실패한 불완전한 파일만 정리합니다.
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                try
                {
                    stream.Write(png, 0, png.Length);
                    stream.Flush();
                }
                catch
                {
                    stream.Dispose();
                    File.Delete(path);
                    throw;
                }
            }
            return path;
        }
    }
}
