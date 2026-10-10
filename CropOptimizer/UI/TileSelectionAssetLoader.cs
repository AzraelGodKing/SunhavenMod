using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Wish;

using SunhavenMods.Shared;
using ReflectionProbe = SunhavenMods.Shared.ReflectionProbe;

namespace CropOptimizer.UI
{
    /// <summary>
    /// Loads <c>Assets/tile_selection_sheet.png</c>: three 36×36 sprites in one row
    /// (yellow corners, green corners, green full outline). A fourth frame (purple corners)
    /// is generated from the yellow one at load time.
    /// </summary>
    internal static class TileSelectionAssetLoader
    {
        public const int FrameYellowCorners = 0;
        public const int FrameGreenCorners = 1;
        public const int FrameGreenOutline = 2;
        public const int FrameManaCorners = 3;

        private const string SheetFileName = "tile_selection_sheet.png";
        private const int SheetSpriteCount = 3;
        private const int SpriteCount = 4;
        private const int SpriteSizePx = 36;
        private static readonly Color ManaShadow = new Color(0.24f, 0.06f, 0.32f);
        private static readonly Color ManaLight = new Color(0.84f, 0.6f, 1f);
        private static readonly Vector4 DefaultSliceBorder = new Vector4(7f, 7f, 7f, 7f);

        private static Texture2D _sheetTexture;
        private static Sprite[] _sprites;
        private static bool _loadAttempted;
        private static Vector4 _sliceBorder = DefaultSliceBorder;
        private static float _pixelsPerUnit = SpriteSizePx / GameFarmCoords.SelectionSpriteSize.x;

        public static bool EnsureLoaded()
        {
            if (_sprites != null && _sprites.Length == SpriteCount)
                return true;

            if (_loadAttempted)
                return _sprites != null;

            _loadAttempted = true;
            TryProbeVanillaSliceMetadata();

            try
            {
                string path = ResolveSheetPath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    ReleaseSheetTexture();
                    return false;
                }

                byte[] bytes = File.ReadAllBytes(path);
                _sheetTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

                if (!ImageConversion.LoadImage(_sheetTexture, bytes))
                {
                    ReleaseSheetTexture();
                    return false;
                }

                if (_sheetTexture.width < SpriteSizePx * SheetSpriteCount || _sheetTexture.height < SpriteSizePx)
                {
                    ReleaseSheetTexture();
                    return false;
                }

                AppendManaFrame();

                _sprites = new Sprite[SpriteCount];
                _sprites[FrameYellowCorners] = CreateSlicedSprite(FrameYellowCorners);
                _sprites[FrameGreenCorners] = CreateSlicedSprite(FrameGreenCorners);
                _sprites[FrameGreenOutline] = CreateSimpleSprite(FrameGreenOutline);
                _sprites[FrameManaCorners] = CreateSlicedSprite(FrameManaCorners);

                Plugin.Log?.LogInfo(
                    $"[TileSelectionAssetLoader] Loaded {SheetFileName} ({_sheetTexture.width}x{_sheetTexture.height}, " +
                    $"border={_sliceBorder}, ppu={_pixelsPerUnit:F2}).");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[TileSelectionAssetLoader] Load failed: {ex.Message}");
                _sprites = null;
                ReleaseSheetTexture();
                return false;
            }
        }

        /// <summary>
        /// Copies the shipped frames into a wider texture and fills the extra slot with the yellow
        /// corners recolored by brightness from <see cref="ManaShadow"/> to <see cref="ManaLight"/>.
        /// </summary>
        private static void AppendManaFrame()
        {
            int shippedWidth = SpriteSizePx * SheetSpriteCount;
            var expanded = new Texture2D(SpriteSizePx * SpriteCount, SpriteSizePx, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            expanded.SetPixels(0, 0, shippedWidth, SpriteSizePx, _sheetTexture.GetPixels(0, 0, shippedWidth, SpriteSizePx));

            Color[] mana = _sheetTexture.GetPixels(FrameYellowCorners * SpriteSizePx, 0, SpriteSizePx, SpriteSizePx);
            for (int i = 0; i < mana.Length; i++)
            {
                Color c = mana[i];
                float brightness = Mathf.Max(c.r, c.g, c.b);
                Color tinted = Color.Lerp(ManaShadow, ManaLight, brightness * brightness);
                tinted.a = c.a;
                mana[i] = tinted;
            }

            expanded.SetPixels(FrameManaCorners * SpriteSizePx, 0, SpriteSizePx, SpriteSizePx, mana);
            expanded.Apply();

            ReleaseSheetTexture();
            _sheetTexture = expanded;
        }

        private static void ReleaseSheetTexture()
        {
            if (_sheetTexture == null)
                return;

            UnityEngine.Object.Destroy(_sheetTexture);
            _sheetTexture = null;
        }

        public static bool TryGetFrame(int index, out Sprite sprite)
        {
            sprite = null;
            if (!EnsureLoaded() || index < 0 || index >= _sprites.Length)
                return false;

            sprite = _sprites[index];
            return sprite != null;
        }

        public static bool UsesSlicedDrawMode(int index)
        {
            return index == FrameYellowCorners || index == FrameGreenCorners || index == FrameManaCorners;
        }

        private static Sprite CreateSlicedSprite(int index)
        {
            return Sprite.Create(
                _sheetTexture,
                BuildRect(index),
                new Vector2(0.5f, 0.5f),
                _pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                _sliceBorder);
        }

        private static Sprite CreateSimpleSprite(int index)
        {
            return Sprite.Create(
                _sheetTexture,
                BuildRect(index),
                new Vector2(0.5f, 0.5f),
                _pixelsPerUnit);
        }

        private static Rect BuildRect(int index)
        {
            float x = index * SpriteSizePx;
            return new Rect(x, 0f, SpriteSizePx, SpriteSizePx);
        }

        private static void TryProbeVanillaSliceMetadata()
        {
            try
            {
                FieldInfo selectionField = AccessTools.Field(typeof(Tool), "_selection");
                UseItem useItem = Player.Instance?.UseItem;
                if (useItem == null || selectionField == null)
                    return;

                if (selectionField.GetValue(useItem) is not GameObject selection || selection == null)
                    return;

                var renderer = selection.GetComponent<SpriteRenderer>();
                Sprite sprite = renderer?.sprite;
                if (sprite == null)
                    return;

                if (sprite.border.sqrMagnitude > 0.01f)
                    _sliceBorder = sprite.border;

                if (sprite.pixelsPerUnit > 0.01f)
                    _pixelsPerUnit = sprite.pixelsPerUnit;
            }
            catch (Exception __ex) { ReflectionProbe.LogOnce("CropOptimizer/UI/TileSelectionAssetLoader.cs:165", __ex, Plugin.Log); }
        }

        private static string ResolveSheetPath()
        {
            try
            {
                string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrEmpty(pluginDir))
                {
                    string besideDll = Path.Combine(pluginDir, "Assets", SheetFileName);
                    if (File.Exists(besideDll))
                        return besideDll;
                }
            }
            catch (Exception __ex) { ReflectionProbe.LogOnce("CropOptimizer/UI/TileSelectionAssetLoader.cs:182", __ex, Plugin.Log); }

            return null;
        }
    }
}
