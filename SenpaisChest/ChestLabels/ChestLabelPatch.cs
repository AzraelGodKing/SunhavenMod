using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Wish;

namespace SenpaisChest.ChestLabels
{
    internal static class ChestLabelPatch
    {
        private static readonly HashSet<Type> AllowedChestTypes = new HashSet<Type>();

        static ChestLabelPatch()
        {
            AllowedChestTypes.Add(typeof(Chest));
            var bankChest = AccessTools.TypeByName("Wish.BankChest");
            if (bankChest != null)
                AllowedChestTypes.Add(bankChest);
        }

        internal static void EnsureLabel(Chest chest)
        {
            var config = Plugin.GetConfig();
            if (config == null || !config.EnableChestLabels.Value) return;
            if (chest == null || chest.gameObject == null) return;
            if (!AllowedChestTypes.Contains(chest.GetType())) return;

            if (!chest.TryGetComponent<ChestLabel>(out var label))
            {
                label = chest.gameObject.AddComponent<ChestLabel>();
                chest.StartCoroutine(InitWhenReady(label));
            }
            else
            {
                label.DoUpdate();
            }
        }

        private static IEnumerator InitWhenReady(ChestLabel label)
        {
            if (label == null) yield break;
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                if (SingletonBehaviour<DayCycle>.Instance != null)
                    break;
            }
            label.Init();
            label.DoUpdate();
        }

        internal static void SetMeta_Postfix(Chest __instance) => EnsureLabel(__instance);
        internal static void OnEnable_Postfix(Chest __instance) => EnsureLabel(__instance);

        internal static void InteractionPoint_Postfix(Chest __instance, ref InteractionInfo __result)
        {
            if (__result == null) return;
            var config = Plugin.GetConfig();
            if (config == null || !config.EnableChestLabels.Value) return;

            var label = __instance.GetComponent<ChestLabel>();
            if (label == null) return;

            var text = label.GetText();
            __result.interactionText = new List<string>
            {
                !string.IsNullOrWhiteSpace(text) ? text : "Chest"
            };
        }
    }
}
