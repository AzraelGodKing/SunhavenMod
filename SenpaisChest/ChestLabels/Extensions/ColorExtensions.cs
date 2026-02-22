using UnityEngine;

namespace SenpaisChest.ChestLabels.Extensions
{
    internal static class ColorExtensions
    {
        public static Color32 ToColor(this int hexVal)
        {
            return new Color32(
                (byte)((hexVal >> 16) & 255),
                (byte)((hexVal >> 8) & 255),
                (byte)(hexVal & 255),
                255);
        }
    }
}
