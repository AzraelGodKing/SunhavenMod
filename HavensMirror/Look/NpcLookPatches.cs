using System;
using System.Reflection;
using HarmonyLib;
using HavensMirror.Config;
using HavensMirror.Gallery;
using HavensMirror.Look;
using SunhavenMods.Shared;

namespace HavensMirror.Look
{
    /// <summary>
    /// When ForcedLook is a seasonal slot, nudge NPC seasonal animator selection.
    /// </summary>
    public static class NpcLookPatches
    {
        public static void Apply(Harmony harmony)
        {
            Type npcType = ReflectionHelper.FindWishType("NPCAI");
            if (npcType == null)
            {
                Plugin.Log?.LogWarning("[Reflect] Wish.NPCAI not found — seasonal outfit override inactive.");
                return;
            }

            MethodInfo load = AccessTools.Method(npcType, "LoadSeasonAnimators");
            if (load == null)
            {
                Plugin.Log?.LogWarning("[Reflect] NPCAI.LoadSeasonAnimators not found.");
                return;
            }

            try
            {
                harmony.Patch(load, prefix: new HarmonyMethod(typeof(NpcLookPatches), nameof(LoadSeasonAnimatorsPrefix)));
                Plugin.Log?.LogInfo("Patched NPCAI.LoadSeasonAnimators for ForcedLook season override.");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Reflect] NPCAI.LoadSeasonAnimators patch failed: {ex.Message}");
            }
        }

        private static bool LoadSeasonAnimatorsPrefix(object __instance)
        {
            if (MirrorOptions.Enabled == null || !MirrorOptions.Enabled.Value)
                return true;

            if (!ForcedLookResolver.TryParseForced(out LookSlot forced))
                return true;

            if (forced == LookSlot.Vows || forced == LookSlot.Shore || forced == LookSlot.Costume)
                return true;

            try
            {
                Type type = __instance.GetType();
                FieldInfo loaderField = AccessTools.Field(type, "seasonAnimatorLoader")
                                        ?? AccessTools.Field(type, "_seasonAnimatorLoader");
                FieldInfo hasSeasonal = AccessTools.Field(type, "hasSeasonalSprites")
                                        ?? AccessTools.Field(type, "_hasSeasonalSprites");
                FieldInfo layerField = AccessTools.Field(type, "currentAnimatorLayer")
                                       ?? AccessTools.Field(type, "_currentAnimatorLayer");
                FieldInfo facingField = AccessTools.Field(type, "_facingDirection")
                                        ?? AccessTools.Field(type, "facingDirection");
                FieldInfo meshField = AccessTools.Field(type, "_meshGenerator")
                                      ?? AccessTools.Field(type, "meshGenerator");
                FieldInfo slugField = AccessTools.Field(type, "_isBSDSluggedNPC")
                                      ?? AccessTools.Field(type, "isBSDSluggedNPC");

                if (slugField != null && slugField.GetValue(__instance) is bool slugged && slugged)
                {
                    MethodInfo slugAnim = AccessTools.Method(type, "GetSluggedAnimator");
                    slugAnim?.Invoke(__instance, null);
                }

                object loader = loaderField?.GetValue(__instance);
                if (loader == null)
                    return false;

                int index = 0;
                bool seasonal = hasSeasonal != null && hasSeasonal.GetValue(__instance) is bool hs && hs;
                if (seasonal)
                    index = ForcedLookResolver.ResolveSeasonAnimatorIndex();

                MethodInfo loadAnimator = AccessTools.Method(loader.GetType(), "LoadAnimator");
                if (loadAnimator == null)
                    return true;

                // Build a callback matching Action / Action<object> if needed — prefer parameterless continuation via DelayOneFrame.
                object callback = CreateAnimatorCallback(__instance, layerField, facingField, meshField);
                if (callback != null && loadAnimator.GetParameters().Length >= 2)
                    loadAnimator.Invoke(loader, new[] { (object)index, callback });
                else if (loadAnimator.GetParameters().Length == 1)
                    loadAnimator.Invoke(loader, new object[] { index });
                else
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[Look] LoadSeasonAnimatorsPrefix fallback to vanilla: {ex.Message}");
                return true;
            }
        }

        private static object CreateAnimatorCallback(
            object npc,
            FieldInfo layerField,
            FieldInfo facingField,
            FieldInfo meshField)
        {
            try
            {
                Type npcType = npc.GetType();
                MethodInfo delay = AccessTools.Method(npcType, "DelayOneFrame", new[] { typeof(Action) });
                if (delay == null)
                    return null;

                Action body = () =>
                {
                    try
                    {
                        var animatorProp = AccessTools.Property(npcType, "animator")
                                           ?? AccessTools.Property(npcType, "Animator");
                        object animator = animatorProp?.GetValue(npc, null);
                        if (animator == null)
                            return;

                        var goProp = AccessTools.Property(animator.GetType(), "gameObject");
                        UnityEngine.GameObject go = goProp?.GetValue(animator, null) as UnityEngine.GameObject;
                        var racProp = AccessTools.Property(animator.GetType(), "runtimeAnimatorController");
                        object rac = racProp?.GetValue(animator, null);
                        if (go == null || !go.activeInHierarchy || rac == null)
                            return;

                        int layerCount = 0;
                        var layerCountProp = AccessTools.Property(animator.GetType(), "layerCount");
                        if (layerCountProp != null)
                            layerCount = (int)layerCountProp.GetValue(animator, null);

                        int currentLayer = 0;
                        if (layerField != null && layerField.GetValue(npc) is int layerVal)
                            currentLayer = layerVal;

                        MethodInfo setWeight = AccessTools.Method(animator.GetType(), "SetLayerWeight", new[] { typeof(int), typeof(float) });
                        if (setWeight != null)
                        {
                            for (int j = 0; j < layerCount; j++)
                                setWeight.Invoke(animator, new object[] { j, j == currentLayer ? 1f : 0f });
                        }

                        object mesh = meshField?.GetValue(npc);
                        MethodInfo setDefault = mesh != null ? AccessTools.Method(mesh.GetType(), "SetDefault") : null;

                        Action meshReset = () =>
                        {
                            try { setDefault?.Invoke(mesh, null); }
                            catch { /* ignore */ }
                        };

                        if (delay != null)
                            delay.Invoke(npc, new object[] { meshReset });

                        object facing = facingField?.GetValue(npc);
                        MethodInfo setInt = AccessTools.Method(animator.GetType(), "SetInteger", new[] { typeof(string), typeof(int) });
                        if (setInt != null && facing != null)
                            setInt.Invoke(animator, new object[] { "Direction", Convert.ToInt32(facing) });
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.LogDebug($"[Look] Animator callback: {ex.Message}");
                    }
                };

                // LoadAnimator expects Action — return the body action for the loader to invoke after load.
                Action outer = () =>
                {
                    try { delay.Invoke(npc, new object[] { body }); }
                    catch (Exception ex) { Plugin.Log?.LogDebug($"[Look] DelayOneFrame: {ex.Message}"); }
                };
                return outer;
            }
            catch
            {
                return null;
            }
        }
    }
}
