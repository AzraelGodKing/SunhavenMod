using System;
using GiftingAssistant.Game;
using HarmonyLib;

namespace GiftingAssistant.Patches
{
    /// <summary>
    /// Postfix on Wish.NPCAI.Gift(Item): when a rostered NPC is gifted, mark them as gifted
    /// today so the window updates and the row sinks immediately.
    /// </summary>
    public static class GiftPatch
    {
        public static void OnGiftGiven(object __instance, object __0)
        {
            try
            {
                if (__instance == null)
                    return;

                var npcType = __instance.GetType();
                var nameProp = AccessTools.Property(npcType, "OriginalName")
                               ?? AccessTools.Property(npcType, "ActualNPCName")
                               ?? AccessTools.Property(npcType, "NPCName")
                               ?? AccessTools.Property(npcType, "npcName")
                               ?? AccessTools.Property(npcType, "Name");

                string npcName = nameProp?.GetValue(__instance)?.ToString();
                npcName = GiftGameData.NormalizeNpcName(npcName);
                if (string.IsNullOrEmpty(npcName))
                    return;

                Plugin.MarkNpcGifted(npcName);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[GiftingAssistant] Error tracking gift: {ex.Message}");
            }
        }
    }
}
