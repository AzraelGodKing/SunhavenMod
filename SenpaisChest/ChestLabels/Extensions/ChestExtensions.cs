using System.Reflection;
using HarmonyLib;
using Wish;

namespace SenpaisChest.ChestLabels.Extensions
{
    internal static class ChestExtensions
    {
        private static readonly FieldInfo DataField = AccessTools.Field(typeof(Chest), "data");

        public static ChestData GetChestData(this Chest chest)
        {
            if (chest == null)
                return new ChestData();

            try
            {
                var data = DataField?.GetValue(chest) as ChestData;
                return data ?? new ChestData();
            }
            catch
            {
                return new ChestData();
            }
        }
    }
}
