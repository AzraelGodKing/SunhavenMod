using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SunhavenMods.Shared
{
    public static class RelationshipHeartAssetLoader
    {
        private static Texture2D _bgStandard;
        private static Texture2D _bgMilestone;
        private static Texture2D _fillStandard;
        private static Texture2D _fillMilestone;
        private static Texture2D _heartIcon;
        private static bool _loadAttempted;

        public static bool IsLoaded =>
            _heartIcon != null && _bgStandard != null && _bgMilestone != null;

        public static Texture2D BgStandard => _bgStandard;
        public static Texture2D BgMilestone => _bgMilestone;
        public static Texture2D FillStandard => _fillStandard;
        public static Texture2D FillMilestone => _fillMilestone;
        public static Texture2D HeartIcon => _heartIcon;

        public static void EnsureLoaded()
        {
            if (_loadAttempted)
                return;
            _loadAttempted = true;

            string dir = ResolveAssetsDirectory();
            if (string.IsNullOrEmpty(dir))
                return;

            _bgStandard = LoadPng(Path.Combine(dir, "hearts_bg_1-20.png"));
            _bgMilestone = LoadPng(Path.Combine(dir, "hearts_bg_21-25.png"));
            _fillStandard = LoadPng(Path.Combine(dir, "hearts_fill_1-10.png"));
            _fillMilestone = LoadPng(Path.Combine(dir, "hearts_fill_21-25.png"));
            _heartIcon = LoadPng(Path.Combine(dir, "heart_icon.png"));
        }

        private static string ResolveAssetsDirectory()
        {
            try
            {
                string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(pluginDir))
                    return null;

                string assets = Path.Combine(pluginDir, "Assets", "Relationships");
                return Directory.Exists(assets) ? assets : null;
            }
            catch
            {
                return null;
            }
        }

        private static Texture2D LoadPng(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                if (!ImageConversion.LoadImage(tex, bytes))
                {
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }
                return tex;
            }
            catch
            {
                return null;
            }
        }
    }
}
