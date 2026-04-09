using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Registers persistent <see cref="GameObject"/> name substrings so a Harmony postfix on
    /// <see cref="Scene.GetRootGameObjects"/> omits matching roots from the returned array.
    /// Sun Haven's unload path uses that list to destroy objects (e.g. <c>UIHandler.UnloadGame</c>);
    /// filtering mimics the community "Keep Alive" pattern without any dependency on that mod.
    /// Each mod assembly keeps its own substring list and uses a unique Harmony ID.
    /// </summary>
    public static class SceneRootSurvivor
    {
        private static readonly object Lock = new object();
        private static readonly List<string> NoKillSubstrings = new List<string>();
        private static Harmony _harmony;

        /// <summary>Registers <paramref name="go"/>'s <see cref="Object.name"/> (full string, case-insensitive contains).</summary>
        public static void TryRegisterPersistentRunnerGameObject(GameObject go)
        {
            if (go == null) return;
            TryAddNoKillListSubstring(go.name);
        }

        /// <summary>Registers a substring matched with <see cref="GameObject.name"/> (ordinal case-insensitive contains).</summary>
        public static void TryAddNoKillListSubstring(string nameSubstring)
        {
            if (string.IsNullOrEmpty(nameSubstring)) return;

            lock (Lock)
            {
                bool exists = false;
                for (int i = 0; i < NoKillSubstrings.Count; i++)
                {
                    if (string.Equals(NoKillSubstrings[i], nameSubstring, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    NoKillSubstrings.Add(nameSubstring);
            }

            EnsurePatched();
        }

        private static void EnsurePatched()
        {
            if (_harmony != null) return;

            lock (Lock)
            {
                if (_harmony != null) return;

                MethodInfo method = AccessTools.Method(typeof(Scene), nameof(Scene.GetRootGameObjects), Type.EmptyTypes);
                if (method == null) return;

                string asmName = typeof(SceneRootSurvivor).Assembly.GetName().Name ?? "Unknown";
                var harmony = new Harmony("SunhavenMods.SceneRootSurvivor." + asmName);
                harmony.Patch(method,
                    postfix: new HarmonyMethod(typeof(SceneRootSurvivor), nameof(OnGetRootGameObjectsPostfix)));
                _harmony = harmony;
            }
        }

        private static void OnGetRootGameObjectsPostfix(ref GameObject[] __result)
        {
            if (__result == null || __result.Length == 0) return;

            List<string> patterns;
            lock (Lock)
            {
                if (NoKillSubstrings.Count == 0) return;
                patterns = new List<string>(NoKillSubstrings);
            }

            var list = new List<GameObject>(__result);
            for (int i = 0; i < patterns.Count; i++)
            {
                string noKill = patterns[i];
                list.RemoveAll(a => a != null && a.name.IndexOf(noKill, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            __result = list.ToArray();
        }
    }
}
