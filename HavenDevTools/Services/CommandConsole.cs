using System;
using System.Collections.Generic;
using System.Reflection;
using SunhavenMods.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wish;

namespace HavenDevTools.Services
{
    /// <summary>
    /// In-game command console for dev commands: spawn, tp, time.set, reload.config, etc.
    /// </summary>
    public class CommandConsole
    {
        private string _input = "";
        private readonly List<string> _output = new List<string>();
        private readonly List<string> _history = new List<string>();
        private int _historyIndex = -1;
        private Vector2 _outputScroll;
        private const int MaxOutputLines = 100;

        public void Draw(GUIStyle boxStyle, GUIStyle buttonStyle, GUIStyle labelStyle, GUIStyle textFieldStyle)
        {
            // Output area
            GUILayout.Label("Output:", labelStyle);
            float outHeight = Mathf.Min(120, 20 + _output.Count * 18);
            _outputScroll = GUILayout.BeginScrollView(_outputScroll, GUILayout.Height(outHeight));
            foreach (var line in _output)
                GUILayout.Label(line, labelStyle);
            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // Input
            GUILayout.BeginHorizontal();
            GUILayout.Label(">", labelStyle, GUILayout.Width(12));
            GUI.SetNextControlName("ConsoleInput");
            _input = GUILayout.TextField(_input, textFieldStyle);
            if (GUILayout.Button("Execute", buttonStyle, GUILayout.Width(70)))
            {
                Execute(_input);
                _input = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Commands: spawn &lt;id&gt; [qty] | tp &lt;scene&gt; | time.set &lt;hour&gt; | reload.config", labelStyle);
        }

        public void Execute(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            command = command.Trim();
            _history.Add(command);
            if (_history.Count > 50) _history.RemoveAt(0);
            _historyIndex = _history.Count;

            string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string cmd = parts[0].ToLowerInvariant();
            try
            {
                switch (cmd)
                {
                    case "spawn":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int itemId))
                        {
                            int qty = parts.Length >= 3 && int.TryParse(parts[2], out int q) ? q : 1;
                            if (Plugin.GetItemInspector()?.SpawnItem(itemId, qty) == true)
                                AddOutput($"Spawned item {itemId} x{qty}");
                            else
                                AddOutput($"Failed to spawn {itemId}");
                        }
                        else
                            AddOutput("Usage: spawn <id> [qty]");
                        break;

                    case "tp":
                    case "teleport":
                        if (parts.Length >= 2)
                        {
                            string scene = parts[1];
                            SceneManager.LoadScene(scene);
                            AddOutput($"Loading scene: {scene}");
                        }
                        else
                            AddOutput("Usage: tp <scene>");
                        break;

                    case "time.set":
                        if (parts.Length >= 2 && float.TryParse(parts[1], out float hour))
                        {
                            if (TrySetTime(hour))
                                AddOutput($"Set time to {hour}");
                            else
                                AddOutput("Could not set time");
                        }
                        else
                            AddOutput("Usage: time.set <hour>");
                        break;

                    case "reload.config":
                        ConfigReloader.ReloadHavenDevToolsConfig();
                        AddOutput("Config reloaded");
                        break;

                    case "player.stat":
                        if (parts.Length >= 3)
                        {
                            if (TrySetPlayerStat(parts[1], parts[2]))
                                AddOutput($"Set {parts[1]} = {parts[2]}");
                            else
                                AddOutput($"Failed to set {parts[1]}");
                        }
                        else
                            AddOutput("Usage: player.stat <stat> <value>");
                        break;

                    default:
                        AddOutput($"Unknown command: {cmd}. Try: spawn, tp, time.set, reload.config");
                        break;
                }
            }
            catch (Exception ex)
            {
                AddOutput($"Error: {ex.Message}");
            }
        }

        private void AddOutput(string line)
        {
            _output.Add(line);
            if (_output.Count > MaxOutputLines)
                _output.RemoveAt(0);
        }

        private static bool TrySetTime(float hour)
        {
            try
            {
                var dayCycleType = SunhavenMods.Shared.ReflectionHelper.FindType("DayCycle", "Wish");
                if (dayCycleType == null) return false;
                var dayCycle = UnityEngine.Object.FindObjectOfType(dayCycleType);
                if (dayCycle == null) return false;
                var type = dayCycle.GetType();
                var prop = type.GetProperty("CurrentTime") ?? type.GetProperty("currentTime");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(dayCycle, hour);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[CommandConsole] TrySetTime: {ex.Message}");
            }
            return false;
        }

        private static bool TrySetPlayerStat(string statName, string valueStr)
        {
            try
            {
                if (Wish.Player.Instance == null) return false;
                var type = Wish.Player.Instance.GetType();
                var prop = type.GetProperty(statName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null || !prop.CanWrite) return false;

                object val = valueStr;
                if (prop.PropertyType == typeof(int) && int.TryParse(valueStr, out int vi))
                    val = vi;
                else if (prop.PropertyType == typeof(float) && float.TryParse(valueStr, out float vf))
                    val = vf;
                else if (prop.PropertyType == typeof(bool))
                    val = valueStr.Equals("true", StringComparison.OrdinalIgnoreCase) || valueStr == "1";

                prop.SetValue(Wish.Player.Instance, Convert.ChangeType(val, prop.PropertyType));
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[CommandConsole] TrySetPlayerStat: {ex.Message}");
            }
            return false;
        }
    }
}
