using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Wish;

namespace CropOptimizer.UI
{
    /// <summary>
    /// Field highlights using bundled tile-selection art or vanilla <c>Tool._selection</c>.
    /// </summary>
    internal sealed class GameSelectionHighlightRenderer
    {
        private const int SortingOrder = 9500;
        private const float IsoYScale = 1.4142135f;

        private static GameObject _prototype;
        private static bool _prototypeResolved;
        private static bool _useBundledSprites;
        private static FieldInfo _selectionField;

        private readonly List<Marker> _pool = new List<Marker>(128);
        private int _activeCount;
        private bool _initialized;

        public void EnsureCreated(Transform unusedParent)
        {
            if (_initialized)
                return;

            _initialized = true;
            ResolvePrototype();
        }

        public void Destroy()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].Root != null)
                    Object.Destroy(_pool[i].Root);
            }

            _pool.Clear();
            _activeCount = 0;
            _initialized = false;
        }

        public void Sync(IReadOnlyList<CropHighlightTarget> targets)
        {
            if (!_initialized)
                return;

            if (!ResolvePrototype())
            {
                HideAll();
                return;
            }

            int targetCount = targets?.Count ?? 0;
            while (_pool.Count < targetCount)
                _pool.Add(CreateMarker());

            int visible = 0;
            for (int i = 0; i < targetCount; i++)
            {
                CropHighlightTarget target = targets[i];
                Marker marker = _pool[visible];
                marker.Root.SetActive(true);
                int frameIndex = FrameIndexForKind(target.Kind);
                marker.ApplyFarmTile(
                    target.Tile,
                    SpriteForKind(target.Kind),
                    ColorForKind(target.Kind),
                    !_useBundledSprites || TileSelectionAssetLoader.UsesSlicedDrawMode(frameIndex));
                visible++;
            }

            for (int i = visible; i < _activeCount; i++)
                _pool[i].Root.SetActive(false);

            _activeCount = visible;
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
                HideAll();
        }

        private void HideAll()
        {
            for (int i = 0; i < _activeCount; i++)
                _pool[i].Root.SetActive(false);
            _activeCount = 0;
        }

        private static bool ResolvePrototype()
        {
            if (_useBundledSprites || _prototype != null)
                return true;

            if (_prototypeResolved)
                return false;

            if (TileSelectionAssetLoader.EnsureLoaded())
            {
                _useBundledSprites = true;
                _prototypeResolved = true;
                return true;
            }

            _useBundledSprites = false;
            _selectionField = AccessTools.Field(typeof(Tool), "_selection");

            try
            {
                UseItem useItem = Player.Instance?.UseItem;
                if (useItem != null && _selectionField != null
                    && _selectionField.GetValue(useItem) is GameObject liveSelection && liveSelection != null)
                {
                    _prototype = ClonePrototype(liveSelection);
                    if (_prototype != null)
                    {
                        _prototypeResolved = true;
                        return true;
                    }
                }

                foreach (UseItem item in Object.FindObjectsOfType<UseItem>())
                {
                    if (item == null || _selectionField == null)
                        continue;
                    if (_selectionField.GetValue(item) is GameObject selection && selection != null)
                    {
                        _prototype = ClonePrototype(selection);
                        if (_prototype != null)
                        {
                            _prototypeResolved = true;
                            return true;
                        }
                    }
                }
            }
            catch
            {
            }

            _prototype = null;
            return false;
        }

        private static GameObject ClonePrototype(GameObject source)
        {
            var clone = Object.Instantiate(source);
            clone.name = "CropOptimizer_SelectionPrototype";
            Object.DontDestroyOnLoad(clone);
            clone.SetActive(false);
            return clone;
        }

        private static Sprite SpriteForKind(CropHighlightKind kind)
        {
            if (!_useBundledSprites)
                return null;

            int frame = kind == CropHighlightKind.NeedsFertilizer
                ? TileSelectionAssetLoader.FrameGreenCorners
                : TileSelectionAssetLoader.FrameYellowCorners;

            return TileSelectionAssetLoader.TryGetFrame(frame, out Sprite sprite) ? sprite : null;
        }

        private static int FrameIndexForKind(CropHighlightKind kind)
        {
            return kind == CropHighlightKind.NeedsFertilizer
                ? TileSelectionAssetLoader.FrameGreenCorners
                : TileSelectionAssetLoader.FrameYellowCorners;
        }

        private static Color ColorForKind(CropHighlightKind kind)
        {
            if (_useBundledSprites)
            {
                return kind == CropHighlightKind.NeedsWater
                    ? new Color(0.98f, 0.98f, 1f, 0.96f)
                    : Color.white;
            }

            return kind == CropHighlightKind.NeedsWater
                ? new Color(0.95f, 0.98f, 1f, 0.94f)
                : new Color(0.55f, 1f, 0.45f, 0.94f);
        }

        private Marker CreateMarker()
        {
            GameObject root;
            SpriteRenderer renderer;

            if (_useBundledSprites)
            {
                root = new GameObject("CropOptimizer_FieldSelection");
                Object.DontDestroyOnLoad(root);
                renderer = root.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = SortingOrder;
            }
            else
            {
                root = Object.Instantiate(_prototype);
                root.name = "CropOptimizer_FieldSelection";
                Object.DontDestroyOnLoad(root);
                renderer = root.GetComponent<SpriteRenderer>();
                if (renderer == null)
                    renderer = root.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = SortingOrder;
            }

            root.SetActive(false);
            return new Marker(root, renderer, _useBundledSprites);
        }

        private sealed class Marker
        {
            public GameObject Root { get; }
            private readonly SpriteRenderer _renderer;
            private readonly bool _bundled;

            public Marker(GameObject root, SpriteRenderer renderer, bool bundled)
            {
                Root = root;
                _renderer = renderer;
                _bundled = bundled;
            }

            public void ApplyFarmTile(Vector2Int farmTile, Sprite sprite, Color color, bool sliced)
            {
                Root.transform.eulerAngles = Vector3.zero;
                Root.transform.localScale = new Vector3(1f, IsoYScale, 1f);
                Root.transform.position = GameFarmCoords.GetSelectionWorldPosition(farmTile);

                if (_bundled && sprite != null)
                    _renderer.sprite = sprite;

                _renderer.drawMode = sliced ? SpriteDrawMode.Sliced : SpriteDrawMode.Simple;
                if (sliced)
                    _renderer.size = GameFarmCoords.SelectionSpriteSize;

                _renderer.color = color;
            }
        }
    }
}
