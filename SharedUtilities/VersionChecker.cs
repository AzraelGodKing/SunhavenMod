#pragma warning disable CS0436 // Type conflicts with imported type - this is our VersionChecker/ReflectionHelper
using System;
using System.Collections;
using System.Net;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using UnityEngine;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Checks for mod updates via GitHub Pages hosted versions.json.
    /// Each mod can call CheckForUpdate() on startup to notify players of new versions.
    /// </summary>
    public static class VersionChecker
    {
        // UPDATE THIS URL to your GitHub Pages URL
        private const string VersionsUrl = "https://azraelgodking.github.io/SunhavenMod/versions.json";

        private static ManualLogSource _logger;

        /// <summary>
        /// Result of a version check operation.
        /// </summary>
        public class VersionCheckResult
        {
            public bool Success { get; set; }
            public bool UpdateAvailable { get; set; }
            public string CurrentVersion { get; set; }
            public string LatestVersion { get; set; }
            public string ModName { get; set; }
            public string NexusUrl { get; set; }
            public string Changelog { get; set; }
            public string ErrorMessage { get; set; }
        }

        /// <summary>
        /// Checks for updates for a specific mod. Call this from your plugin's Awake or Start.
        /// </summary>
        /// <param name="pluginGuid">The PLUGIN_GUID of your mod</param>
        /// <param name="currentVersion">The current PLUGIN_VERSION</param>
        /// <param name="logger">Optional logger for debug output</param>
        /// <param name="onComplete">Callback when check completes</param>
        public static void CheckForUpdate(string pluginGuid, string currentVersion, ManualLogSource logger = null, Action<VersionCheckResult> onComplete = null)
        {
            _logger = logger;

            // Use a MonoBehaviour to run the coroutine
            var runner = new GameObject("VersionChecker").AddComponent<VersionCheckRunner>();
            UnityEngine.Object.DontDestroyOnLoad(runner.gameObject);
            runner.StartCheck(pluginGuid, currentVersion, onComplete);
        }

        /// <summary>
        /// Compares two semantic version strings.
        /// Returns: -1 if v1 &lt; v2, 0 if equal, 1 if v1 &gt; v2
        /// </summary>
        public static int CompareVersions(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1) || string.IsNullOrEmpty(v2))
                return 0;

            // Remove 'v' prefix if present
            v1 = v1.TrimStart('v', 'V');
            v2 = v2.TrimStart('v', 'V');

            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            var maxLength = Math.Max(parts1.Length, parts2.Length);

            for (int i = 0; i < maxLength; i++)
            {
                int num1 = i < parts1.Length && int.TryParse(parts1[i], out var n1) ? n1 : 0;
                int num2 = i < parts2.Length && int.TryParse(parts2[i], out var n2) ? n2 : 0;

                if (num1 < num2) return -1;
                if (num1 > num2) return 1;
            }

            return 0;
        }

        internal static void Log(string message)
        {
            _logger?.LogInfo($"[VersionChecker] {message}");
        }

        internal static void LogWarning(string message)
        {
            _logger?.LogWarning($"[VersionChecker] {message}");
        }

        internal static void LogError(string message)
        {
            _logger?.LogError($"[VersionChecker] {message}");
        }

        /// <summary>
        /// Helper MonoBehaviour to run the version check coroutine.
        /// </summary>
        private class VersionCheckRunner : MonoBehaviour
        {
            public void StartCheck(string pluginGuid, string currentVersion, Action<VersionCheckResult> onComplete)
            {
                StartCoroutine(CheckVersionCoroutine(pluginGuid, currentVersion, onComplete));
            }

            private IEnumerator CheckVersionCoroutine(string pluginGuid, string currentVersion, Action<VersionCheckResult> onComplete)
            {
                var result = new VersionCheckResult
                {
                    CurrentVersion = currentVersion
                };

                // Use UnityWebRequest for better compatibility
                using (var www = UnityEngine.Networking.UnityWebRequest.Get(VersionsUrl))
                {
                    www.timeout = 10;

                    yield return www.SendWebRequest();

                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                        www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Network error: {www.error}";
                        LogWarning(result.ErrorMessage);
                        onComplete?.Invoke(result);
                        Destroy(gameObject);
                        yield break;
                    }

                    try
                    {
                        var json = www.downloadHandler.text;

                        // Simple JSON parsing without external dependencies
                        // Look for the mod's entry in the JSON
                        var modPattern = $"\"{Regex.Escape(pluginGuid)}\"\\s*:\\s*\\{{([^}}]+)\\}}";
                        var modMatch = Regex.Match(json, modPattern, RegexOptions.Singleline);

                        if (!modMatch.Success)
                        {
                            result.Success = false;
                            result.ErrorMessage = $"Mod '{pluginGuid}' not found in versions.json";
                            LogWarning(result.ErrorMessage);
                            onComplete?.Invoke(result);
                            Destroy(gameObject);
                            yield break;
                        }

                        var modJson = modMatch.Groups[1].Value;

                        // Extract fields
                        result.LatestVersion = ExtractJsonString(modJson, "version");
                        result.ModName = ExtractJsonString(modJson, "name");
                        result.NexusUrl = ExtractJsonString(modJson, "nexus");
                        result.Changelog = ExtractJsonString(modJson, "changelog");

                        if (string.IsNullOrEmpty(result.LatestVersion))
                        {
                            result.Success = false;
                            result.ErrorMessage = "Could not parse version from response";
                            LogWarning(result.ErrorMessage);
                            onComplete?.Invoke(result);
                            Destroy(gameObject);
                            yield break;
                        }

                        result.Success = true;
                        result.UpdateAvailable = CompareVersions(currentVersion, result.LatestVersion) < 0;

                        if (result.UpdateAvailable)
                        {
                            Log($"Update available for {result.ModName}: {currentVersion} -> {result.LatestVersion}");
                        }
                        else
                        {
                            Log($"{result.ModName} is up to date (v{currentVersion})");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Parse error: {ex.Message}";
                        LogError(result.ErrorMessage);
                    }
                }

                onComplete?.Invoke(result);
                Destroy(gameObject);
            }

            private string ExtractJsonString(string json, string key)
            {
                // Handle both "key": "value" and "key": null
                var pattern = $"\"{key}\"\\s*:\\s*(?:\"([^\"]*)\"|null)";
                var match = Regex.Match(json, pattern);
                return match.Success ? match.Groups[1].Value : null;
            }
        }
    }

    /// <summary>
    /// Extension methods for easy integration with BepInEx plugins.
    /// </summary>
    public static class VersionCheckerExtensions
    {
        /// <summary>
        /// Sends an in-game notification about an available update.
        /// Uses the native Sun Haven notification system if available.
        /// </summary>
        public static void NotifyUpdateAvailable(this SunhavenMods.Shared.VersionChecker.VersionCheckResult result, ManualLogSource logger = null)
        {
            if (!result.UpdateAvailable)
                return;

            var message = $"{result.ModName} update available: v{result.LatestVersion}";

            // Try to use native notification system
            try
            {
                var notificationStackType = SunhavenMods.Shared.ReflectionHelper.FindWishType("NotificationStack");
                if (notificationStackType != null)
                {
                    // Use reflection to get SingletonBehaviour<NotificationStack>.Instance
                    var singletonBaseType = SunhavenMods.Shared.ReflectionHelper.FindType("SingletonBehaviour`1", "Wish");
                    if (singletonBaseType != null)
                    {
                        var singletonType = singletonBaseType.MakeGenericType(notificationStackType);
                        var instanceProp = singletonType.GetProperty("Instance");
                        var instance = instanceProp?.GetValue(null);

                        if (instance != null)
                        {
                            var sendMethod = notificationStackType.GetMethod("SendNotification",
                                new[] { typeof(string), typeof(int), typeof(int), typeof(bool), typeof(bool) });

                            if (sendMethod != null)
                            {
                                // SendNotification(message, itemId, amount, showInChat, playSound)
                                sendMethod.Invoke(instance, new object[] { message, 0, 1, false, true });
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Failed to send native notification: {ex.Message}");
            }

            // Fallback to log message
            logger?.LogWarning($"[UPDATE AVAILABLE] {message}");
            if (!string.IsNullOrEmpty(result.NexusUrl))
            {
                logger?.LogWarning($"Download at: {result.NexusUrl}");
            }
        }
    }
}
