using System.Collections.Generic;
using TheVault.Vault;
using UnityEngine;

namespace TheVault.UI
{
    /// <summary>
    /// Persistent HUD display showing vault currency totals.
    /// Displays as a horizontal bar that can be toggled on/off.
    /// </summary>
    public class VaultHUD : MonoBehaviour
    {
        private VaultManager _vaultManager;
        private bool _isEnabled = true;
        private bool _stylesInitialized;

        // Position settings
        private HUDPosition _position = HUDPosition.TopLeft;
        private float _opacity = 0.95f;

        // Styling
        private GUIStyle _hudBackgroundStyle;
        private GUIStyle _currencyLabelStyle;
        private GUIStyle _currencyValueStyle;
        private GUIStyle _categoryLabelStyle;
        private Texture2D _hudBackground;

        // Colors matching the main vault UI
        private Color _bgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        private readonly Color _textColor = new Color(0.9f, 0.9f, 0.95f);
        private readonly Color _goldColor = new Color(0.95f, 0.8f, 0.3f);
        private readonly Color _accentColor = new Color(0.4f, 0.7f, 0.95f);

        // HUD dimensions
        private const float HUD_HEIGHT = 44f;
        private const float PADDING = 16f;
        private const float ITEM_SPACING = 20f;
        private const float CATEGORY_SPACING = 32f;
        private const float MIN_HUD_WIDTH = 200f;

        // Cache for currency display
        private Dictionary<string, int> _cachedCurrencies = new Dictionary<string, int>();
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.5f; // Update every 0.5 seconds

        public enum HUDPosition
        {
            TopLeft,
            TopCenter,
            TopRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

        public void Initialize(VaultManager vaultManager)
        {
            _vaultManager = vaultManager;
            Plugin.Log?.LogInfo("VaultHUD initialized");
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
        }

        public void SetPosition(HUDPosition position)
        {
            _position = position;
        }

        public void SetOpacity(float opacity)
        {
            _opacity = Mathf.Clamp01(opacity);
            _bgColor.a = _opacity;
            _stylesInitialized = false; // Force style refresh
        }

        public bool IsEnabled => _isEnabled;

        public void Toggle()
        {
            _isEnabled = !_isEnabled;
            Plugin.Log?.LogInfo($"VaultHUD toggled: {(_isEnabled ? "ON" : "OFF")}");
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _hudBackground = MakeTex(2, 2, new Color(_bgColor.r, _bgColor.g, _bgColor.b, _opacity));

            _hudBackgroundStyle = new GUIStyle()
            {
                normal = { background = _hudBackground },
                padding = new RectOffset((int)PADDING, (int)PADDING, 8, 8)
            };

            _currencyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _textColor },
                padding = new RectOffset(0, 6, 0, 0)
            };

            _currencyValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _goldColor },
                padding = new RectOffset(0, 0, 0, 0)
            };

            _categoryLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _accentColor },
                padding = new RectOffset(0, 8, 0, 0)
            };

            _stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            var tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void Update()
        {
            // Periodically update cached currencies
            if (_isEnabled && _vaultManager != null && Time.time - _lastUpdateTime > UPDATE_INTERVAL)
            {
                _cachedCurrencies = _vaultManager.GetAllNonZeroCurrencies();
                _lastUpdateTime = Time.time;
            }
        }

        private void OnGUI()
        {
            if (!_isEnabled || _vaultManager == null) return;

            // Don't show HUD if main vault window is open
            var vaultUI = Plugin.GetVaultUI();
            if (vaultUI != null && vaultUI.IsVisible) return;

            InitializeStyles();

            // Calculate HUD width based on content
            float hudWidth = CalculateHUDWidth();

            // Ensure minimum width so HUD always shows
            hudWidth = Mathf.Max(hudWidth, MIN_HUD_WIDTH);

            // Get position
            Rect hudRect = GetHUDRect(hudWidth);

            // Draw background
            GUI.Box(hudRect, "", _hudBackgroundStyle);

            // Draw content
            GUILayout.BeginArea(new Rect(hudRect.x + PADDING, hudRect.y + 6, hudRect.width - PADDING * 2, hudRect.height - 12));
            GUILayout.BeginHorizontal();

            var seasonal = GetSeasonalTokens();
            var keys = GetKeys();
            var special = GetSpecialCurrencies();

            if (seasonal.Count == 0 && keys.Count == 0 && special.Count == 0)
            {
                // Show placeholder when vault is empty
                GUILayout.Label("The Vault: Empty", _categoryLabelStyle);
            }
            else
            {
                DrawCurrencyGroup("Tokens", seasonal);
                DrawCurrencyGroup("Keys", keys);
                DrawCurrencyGroup("Special", special);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private float CalculateHUDWidth()
        {
            float width = PADDING * 2;

            var seasonal = GetSeasonalTokens();
            var keys = GetKeys();
            var special = GetSpecialCurrencies();

            if (seasonal.Count > 0)
                width += CalculateGroupWidth("Tokens", seasonal) + CATEGORY_SPACING;
            if (keys.Count > 0)
                width += CalculateGroupWidth("Keys", keys) + CATEGORY_SPACING;
            if (special.Count > 0)
                width += CalculateGroupWidth("Special", special);

            return width;
        }

        private float CalculateGroupWidth(string label, Dictionary<string, int> items)
        {
            if (items.Count == 0) return 0;

            float width = label.Length * 9 + 12; // Category label (larger font)

            foreach (var kvp in items)
            {
                string shortName = GetShortName(kvp.Key);
                string value = kvp.Value.ToString();
                width += shortName.Length * 8 + value.Length * 10 + ITEM_SPACING;
            }

            return width;
        }

        private Rect GetHUDRect(float width)
        {
            float x = 0, y = 0;

            switch (_position)
            {
                case HUDPosition.TopLeft:
                    x = 10;
                    y = 10;
                    break;
                case HUDPosition.TopCenter:
                    x = (Screen.width - width) / 2;
                    y = 10;
                    break;
                case HUDPosition.TopRight:
                    x = Screen.width - width - 10;
                    y = 10;
                    break;
                case HUDPosition.BottomLeft:
                    x = 10;
                    y = Screen.height - HUD_HEIGHT - 10;
                    break;
                case HUDPosition.BottomCenter:
                    x = (Screen.width - width) / 2;
                    y = Screen.height - HUD_HEIGHT - 10;
                    break;
                case HUDPosition.BottomRight:
                    x = Screen.width - width - 10;
                    y = Screen.height - HUD_HEIGHT - 10;
                    break;
            }

            return new Rect(x, y, width, HUD_HEIGHT);
        }

        private void DrawCurrencyGroup(string label, Dictionary<string, int> items)
        {
            if (items.Count == 0) return;

            GUILayout.Label(label + ":", _categoryLabelStyle);

            foreach (var kvp in items)
            {
                string shortName = GetShortName(kvp.Key);
                GUILayout.Label(shortName, _currencyLabelStyle);
                GUILayout.Label(kvp.Value.ToString(), _currencyValueStyle);
                GUILayout.Space(ITEM_SPACING);
            }

            GUILayout.Space(CATEGORY_SPACING - ITEM_SPACING);
        }

        private Dictionary<string, int> GetSeasonalTokens()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in _cachedCurrencies)
            {
                if (kvp.Key.StartsWith("seasonal_") && kvp.Value > 0)
                    result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        private Dictionary<string, int> GetKeys()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in _cachedCurrencies)
            {
                if (kvp.Key.StartsWith("key_") && kvp.Value > 0)
                    result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        private Dictionary<string, int> GetSpecialCurrencies()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in _cachedCurrencies)
            {
                if (kvp.Key.StartsWith("special_") && kvp.Value > 0)
                    result[kvp.Key] = kvp.Value;
            }
            return result;
        }

        private string GetShortName(string currencyId)
        {
            if (currencyId.StartsWith("seasonal_"))
            {
                string season = currencyId.Substring("seasonal_".Length);
                return season switch
                {
                    "Spring" => "Sp",
                    "Summer" => "Su",
                    "Fall" => "Fa",
                    "Winter" => "Wi",
                    _ => season.Substring(0, 2)
                };
            }
            else if (currencyId.StartsWith("key_"))
            {
                string key = currencyId.Substring("key_".Length);
                return key switch
                {
                    "copper" => "Cu",
                    "iron" => "Fe",
                    "adamant" => "Ad",
                    "mithril" => "Mi",
                    "sunite" => "Su",
                    "glorite" => "Gl",
                    "kingslostmine" => "KL",
                    _ => key.Substring(0, 2)
                };
            }
            else if (currencyId.StartsWith("special_"))
            {
                string special = currencyId.Substring("special_".Length);
                return special switch
                {
                    "communitytoken" => "CT",
                    "doubloon" => "Db",
                    "blackbottlecap" => "BB",
                    "redcarnivalticket" => "RC",
                    "candycornpieces" => "CC",
                    "manashard" => "MS",
                    _ => special.Substring(0, 2).ToUpper()
                };
            }

            return "??";
        }
    }
}
