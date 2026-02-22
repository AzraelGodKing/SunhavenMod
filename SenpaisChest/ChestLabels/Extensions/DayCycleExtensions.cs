using System.Reflection;
using HarmonyLib;
using TMPro;
using Wish;

namespace SenpaisChest.ChestLabels.Extensions
{
    internal static class DayCycleExtensions
    {
        private static readonly FieldInfo YearTextField = AccessTools.Field(typeof(DayCycle), "_yearTMP");

        public static TextMeshProUGUI GetYearUI(this DayCycle dayCycle)
        {
            if (dayCycle == null)
                return null;

            try
            {
                return YearTextField?.GetValue(dayCycle) as TextMeshProUGUI;
            }
            catch
            {
                return null;
            }
        }
    }
}
