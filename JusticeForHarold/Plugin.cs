using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using Wish;

namespace JusticeForHarold
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"Loading {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}");
            _harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            _harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(NPCAI), nameof(NPCAI.Interact), new[] { typeof(int) })]
    public static class HaroldInteractPatch
    {
        private const int FishingNetItemId = 10502;
        private const string ProgressKey = "JusticeForHaroldComplete";
        private const float RelationshipReward = 2f;

        [HarmonyPrefix]
        public static void Prefix(NPCAI __instance, int interactType)
        {
            if (interactType != 0) return;
            if (__instance.OriginalName != "Harold") return;
            if (SingletonBehaviour<GameSave>.Instance.GetProgressBoolCharacter(ProgressKey)) return;

            var node = BuildHaroldDialogue(__instance);
            __instance.OverrideDialogue(node);
        }

        private static DialogueNode BuildHaroldDialogue(NPCAI npc)
        {
            var main = new DialogueNode();
            main.dialogueText = new List<string>
            {
                "I've lost my fishing nets and I'm in a real bind. Could you spare a fishing net if you have one?"
            };
            var responses = new Dictionary<int, Response>();
            int key = 0;

            if (Player.Instance?.Inventory != null && Player.Instance.Inventory.HasEnough(FishingNetItemId, 1))
            {
                var give = new Response();
                give.responseText = () => "Here, take this.";
                give.action = () => OnGiveNets(npc);
                responses.Add(key++, give);
            }

            var decline = new Response();
            bool hasNets = Player.Instance?.Inventory != null && Player.Instance.Inventory.HasEnough(FishingNetItemId, 1);
            decline.responseText = () => hasNets ? "Maybe another time." : "I don't have any.";
            decline.action = () =>
            {
                npc.CancelDialogueOverride();
                npc.EndInteract(0);
            };
            responses.Add(key, decline);
            main.responses = responses;
            return main;
        }

        private static void OnGiveNets(NPCAI npc)
        {
            if (Player.Instance?.Inventory == null) return;
            if (!Player.Instance.Inventory.HasEnough(FishingNetItemId, 1)) return;

            Player.Instance.Inventory.RemoveItem(FishingNetItemId, 1, 0);
            SingletonBehaviour<GameSave>.Instance.SetProgressBoolCharacter(ProgressKey, true);
            npc.AddRelationship(RelationshipReward, 0f, true);
            npc.CancelDialogueOverride();

            var thankYou = new DialogueNode();
            thankYou.dialogueText = new List<string> { "Thank you so much! I really appreciate it. You've saved my day." };
            var thankResponses = new Dictionary<int, Response>();
            var bye = new Response();
            bye.responseText = () => "You're welcome!";
            bye.action = () => npc.EndInteract(0);
            thankResponses.Add(0, bye);
            thankYou.responses = thankResponses;
            DialogueController.Instance.PushDialogue(thankYou, null, true, false);
        }
    }
}
