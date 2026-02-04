using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SunhavenMods.Shared
{
    /// <summary>
    /// Base class for persistent MonoBehaviour runners that survive Unity scene loads.
    /// Derived classes should override OnUpdate() and OnMenuTransition() for mod-specific logic.
    ///
    /// Usage:
    /// 1. Create a derived class: public class MyRunner : PersistentRunnerBase
    /// 2. Override OnUpdate() for per-frame logic
    /// 3. Override OnMenuTransition() for cleanup when returning to menu
    /// 4. Call CreateRunner&lt;MyRunner&gt;() from your Plugin to instantiate
    /// </summary>
    public abstract class PersistentRunnerBase : MonoBehaviour
    {
        private bool _wasInGame = false;
        private float _lastHeartbeat = 0f;
        private string _lastSceneName = "";

        /// <summary>
        /// Interval between heartbeat logs (seconds). Set to 0 to disable.
        /// Default: 0 (disabled for non-dev mods)
        /// </summary>
        protected virtual float HeartbeatInterval => 0f;

        /// <summary>
        /// Name used in log messages. Override to customize.
        /// </summary>
        protected virtual string RunnerName => GetType().Name;

        /// <summary>
        /// Called every frame. Override for mod-specific update logic.
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Called when transitioning from game to menu. Override for cleanup.
        /// </summary>
        protected virtual void OnMenuTransition() { }

        /// <summary>
        /// Called when transitioning from menu to game. Override for initialization.
        /// </summary>
        protected virtual void OnGameTransition() { }

        /// <summary>
        /// Log a message. Override to use your mod's logger.
        /// </summary>
        protected virtual void Log(string message) { }

        /// <summary>
        /// Log a warning. Override to use your mod's logger.
        /// </summary>
        protected virtual void LogWarning(string message) { }

        /// <summary>
        /// Creates and configures a persistent runner instance.
        /// </summary>
        public static T CreateRunner<T>() where T : PersistentRunnerBase
        {
            var go = new GameObject($"[{typeof(T).Name}]");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            return go.AddComponent<T>();
        }

        protected virtual void Awake()
        {
            _lastSceneName = SceneManager.GetActiveScene().name;
            _wasInGame = !SceneHelpers.IsMenuScene(_lastSceneName);
            Log($"[{RunnerName}] Initialized in scene: {_lastSceneName}");
        }

        protected virtual void Update()
        {
            // Heartbeat logging (optional, for dev tools)
            if (HeartbeatInterval > 0f)
            {
                _lastHeartbeat += Time.deltaTime;
                if (_lastHeartbeat >= HeartbeatInterval)
                {
                    _lastHeartbeat = 0f;
                    Log($"[{RunnerName}] Heartbeat - still alive");
                }
            }

            // Scene transition detection (backup polling)
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != _lastSceneName)
            {
                _lastSceneName = currentScene;
                HandleSceneChange(currentScene);
            }

            // Delegate to derived class
            try
            {
                OnUpdate();
            }
            catch (Exception ex)
            {
                LogWarning($"[{RunnerName}] Error in OnUpdate: {ex.Message}");
            }
        }

        private void HandleSceneChange(string sceneName)
        {
            bool isNowMenu = SceneHelpers.IsMenuScene(sceneName);

            if (_wasInGame && isNowMenu)
            {
                Log($"[{RunnerName}] Menu transition detected");
                try
                {
                    OnMenuTransition();
                }
                catch (Exception ex)
                {
                    LogWarning($"[{RunnerName}] Error in OnMenuTransition: {ex.Message}");
                }
            }
            else if (!_wasInGame && !isNowMenu)
            {
                Log($"[{RunnerName}] Game transition detected");
                try
                {
                    OnGameTransition();
                }
                catch (Exception ex)
                {
                    LogWarning($"[{RunnerName}] Error in OnGameTransition: {ex.Message}");
                }
            }

            _wasInGame = !isNowMenu;
        }

        protected virtual void OnDestroy()
        {
            LogWarning($"[{RunnerName}] OnDestroy called - this should NOT happen!");
        }
    }
}
