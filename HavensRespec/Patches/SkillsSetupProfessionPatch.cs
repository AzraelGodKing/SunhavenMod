using System;
using HarmonyLib;
using HavensRespec.UI;
using Wish;

namespace HavensRespec.Patches
{
    /// <summary>
    /// Harmony postfix on <c>Skills.SetupProfession(ProfessionType, SkillTree, SkillTreeAsset)</c>.
    /// Fires once per tab when the game builds the Skills UI, giving us a chance to inject our
    /// Reset / Undo buttons onto each profession panel.
    /// </summary>
    [HarmonyPatch(typeof(Skills), "SetupProfession")]
    internal static class SkillsSetupProfessionPatch
    {
        public static Action<Skills, ProfessionType, SkillTree> OnSetup;

        [HarmonyPostfix]
        private static void Postfix(Skills __instance, ProfessionType profession, SkillTree panel)
        {
            try
            {
                OnSetup?.Invoke(__instance, profession, panel);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[Respec] SetupProfession postfix threw: {ex}");
            }
        }
    }
}
