using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HavenDevTools.Config;
using SunhavenMods.Shared;
using UnityEngine;

namespace HavenDevTools.UI
{
    /// <summary>
    /// In-game log viewer - ring buffer of recent log entries with filtering.
    /// </summary>
    public class LogViewerPanel
    {
        private static readonly List<LogEntry> _entries = new List<LogEntry>();
        private static readonly object _lock = new object();
        private Vector2 _scrollPosition;
        private int _levelFilterIndex = 1; // 0=Debug, 1=Info, 2=Warning, 3=Error
        private static readonly string[] _levelNames = { "Debug", "Info", "Warning", "Error" };
        private static bool _listenerAttached;

        private static readonly LogCaptureListener _captureListener = new LogCaptureListener();

        public LogViewerPanel()
        {
            if (!_listenerAttached)
            {
                BepInEx.Logging.Logger.Listeners.Add(_captureListener);
                _listenerAttached = true;
            }
        }

        private class LogCaptureListener : ILogListener
        {
            public void LogEvent(object sender, LogEventArgs eventArgs)
            {
                OnLog(eventArgs);
            }

            public void Dispose() { }
        }

        private static void OnLog(LogEventArgs e)
        {
            var level = e.Level switch
            {
                LogLevel.Debug => 0,
                LogLevel.Info => 1,
                LogLevel.Warning => 2,
                LogLevel.Error => 3,
                _ => 1
            };

            lock (_lock)
            {
                _entries.Add(new LogEntry
                {
                    Level = level,
                    Message = e.Data?.ToString() ?? "",
                    Source = e.Source.SourceName,
                    Timestamp = DateTime.Now
                });
                int max = ModConfig.MaxLogEntries?.Value ?? 500;
                while (_entries.Count > max)
                    _entries.RemoveAt(0);
            }
        }

        public void Draw(GUIStyle boxStyle, GUIStyle buttonStyle, GUIStyle labelStyle, float listHeight = 200f)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(ModLocalization.T("devtools.log.level"), labelStyle, GUILayout.Width(40));
            _levelFilterIndex = GUILayout.Toolbar(_levelFilterIndex, _levelNames, buttonStyle);
            if (GUILayout.Button(ModLocalization.T("devtools.log.export"), buttonStyle, GUILayout.Width(60)))
                ExportToFile();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            List<LogEntry> snapshot;
            lock (_lock)
            {
                snapshot = new List<LogEntry>(_entries);
            }

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(listHeight));

            int minLevel = _levelFilterIndex;
            foreach (var e in snapshot)
            {
                if (e.Level < minLevel) continue;
                var color = e.Level switch
                {
                    0 => new Color(0.6f, 0.6f, 0.6f),
                    1 => new Color(0.9f, 0.9f, 0.9f),
                    2 => new Color(1f, 0.85f, 0.3f),
                    3 => new Color(1f, 0.4f, 0.4f),
                    _ => Color.white
                };
                var style = new GUIStyle(labelStyle) { normal = { textColor = color } };
                string line = $"[{e.Timestamp:HH:mm:ss}] [{_levelNames[e.Level]}] [{e.Source}] {e.Message}";
                GUILayout.Label(line, style);
            }

            GUILayout.EndScrollView();
        }

        private static void ExportToFile()
        {
            try
            {
                string path = System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "LogExport.txt");
                List<LogEntry> snapshot;
                lock (_lock)
                {
                    snapshot = new List<LogEntry>(_entries);
                }
                var lines = new List<string>();
                foreach (var e in snapshot)
                {
                    lines.Add($"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] [{_levelNames[e.Level]}] [{e.Source}] {e.Message}");
                }
                System.IO.File.WriteAllLines(path, lines);
                Plugin.Log?.LogInfo($"[LogViewer] Exported {lines.Count} lines to {path}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[LogViewer] Export failed: {ex.Message}");
            }
        }

        private class LogEntry
        {
            public int Level;
            public string Message;
            public string Source;
            public DateTime Timestamp;
        }
    }
}
