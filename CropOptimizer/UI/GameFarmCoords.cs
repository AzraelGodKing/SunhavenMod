using CropOptimizer.Patches;
using UnityEngine;
using Wish;

namespace CropOptimizer.UI
{
    /// <summary>Farm tile coordinates and selection-marker placement (matches vanilla <see cref="Tool"/>).</summary>
    internal static class GameFarmCoords
    {
        private const float IsoYScale = 1.4142135f;
        private static readonly Vector2 SelectionSize = Vector2.one * 1.25f;
        private static readonly Vector3 SelectionOffset = new Vector3(0f, -0.25f, -0.25f);

        public static Vector2 SelectionSpriteSize => SelectionSize;

        /// <summary>1× farm tile under the mouse — same grid as <c>WateringCan</c> / <c>Tool.SetSelectionOnTile</c>.</summary>
        public static Vector2Int GetMouseFarmTile()
        {
            Vector2 mouse = Utilities.MousePositionFloat();
            return new Vector2Int(Mathf.FloorToInt(mouse.x), Mathf.FloorToInt(mouse.y));
        }

        /// <summary>Convert a crop grid position to the 1× farm tile index tools use.</summary>
        public static Vector2Int ToFarmTile(Vector2Int gridPos)
        {
            if (Mathf.Abs(gridPos.x) >= 6 || Mathf.Abs(gridPos.y) >= 6)
                return new Vector2Int(gridPos.x / 6, gridPos.y / 6);
            return gridPos;
        }

        public static bool TryGetCropFarmTile(Component crop, out Vector2Int farmTile)
        {
            farmTile = default;
            if (crop == null)
                return false;

            if (CropGrowthPatch.TryGetCropGridPosition(crop, out Vector2Int gridPos))
            {
                farmTile = ToFarmTile(gridPos);
                return true;
            }

            if (CropTileReflection.TryGetTileCoordForCrop(crop, out Vector2Int alt))
            {
                farmTile = ToFarmTile(alt);
                return true;
            }

            return false;
        }

        /// <summary>World position for the vanilla tool selection bracket on a farm tile.</summary>
        public static Vector3 GetSelectionWorldPosition(Vector2Int farmTile)
        {
            Vector3 vector = new Vector3(farmTile.x + 0.5f, (farmTile.y + 0.5f) * IsoYScale, 0f);
            try
            {
                GameManager gm = SingletonBehaviour<GameManager>.Instance;
                if (gm != null)
                {
                    float depth = gm.Depth(vector);
                    vector = new Vector3(vector.x, vector.y + depth, vector.z + depth);
                }
            }
            catch
            {
            }

            return vector + SelectionOffset;
        }

        public static bool IsCropOnFarmTile(Component crop, Vector2Int farmTile)
        {
            if (crop == null)
                return false;

            if (TryGetCropFarmTile(crop, out Vector2Int cropTile) && cropTile == farmTile)
                return true;

            Vector3 expected = GetSelectionWorldPosition(farmTile);
            Vector3 actual = crop.transform.position;
            float dx = actual.x - expected.x;
            float dy = actual.y - expected.y;
            return dx * dx + dy * dy <= 0.45f * 0.45f;
        }
    }
}
