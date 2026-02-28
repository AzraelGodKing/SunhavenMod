using System.Collections.Generic;
using SunhavenMods.Shared;
using TheVault.Patches;
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

        // HUD dimensions - base values, scaled by _scale
        private const float BASE_HUD_HEIGHT = 28f;
        private const float BASE_PADDING = 8f;
        private const float BASE_ITEM_SPACING = 12f;
        private const float BASE_GROUP_SPACING = 16f;
        private const float MIN_HUD_WIDTH = 100f;

        // Separator styling
        private Texture2D _separatorTexture;
        private readonly Color _separatorColor = new Color(0.4f, 0.4f, 0.5f, 0.5f);

        // Cache for currency display
        private Dictionary<string, int> _cachedCurrencies = new Dictionary<string, int>();
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.5f; // Update every 0.5 seconds

        // Cached grouped dictionaries (refreshed in Update to avoid per-frame allocations)
        private readonly Dictionary<string, int> _cachedSeasonal = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _cachedKeys = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _cachedSpecial = new Dictionary<string, int>();

        // Global scale factor for HUD (for high resolutions / accessibility)
        private float _scale = 1f;

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

        public void SetScale(float scale)
        {
            _scale = Mathf.Clamp(scale, 0.5f, 3f);
            _stylesInitialized = false; // fonts / paddings depend on scale
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
            _separatorTexture = MakeTex(1, 1, _separatorColor);

            float padding = BASE_PADDING * _scale;
            int fontSmall = Mathf.Max(8, Mathf.RoundToInt(11 * _scale));
            int fontMedium = Mathf.Max(9, Mathf.RoundToInt(12 * _scale));

            _hudBackgroundStyle = new GUIStyle()
            {
                normal = { background = _hudBackground },
                padding = new RectOffset((int)padding, (int)padding, 4, 4)
            };

            _currencyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSmall,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _textColor },
                padding = new RectOffset(0, 3, 0, 0)
            };

            _currencyValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontMedium,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _goldColor },
                padding = new RectOffset(1, 0, 0, 0)
            };

            _categoryLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(8, Mathf.RoundToInt(10 * _scale)),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = _accentColor },
                padding = new RectOffset(0, 4, 0, 0)
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
            // Periodically update cached currencies and grouped caches
            if (_isEnabled && _vaultManager != null && Time.time - _lastUpdateTime > UPDATE_INTERVAL)
            {
                _cachedCurrencies = _vaultManager.GetAllNonZeroCurrencies();
                RefreshGroupedCaches();
                _lastUpdateTime = Time.time;
            }
        }

        private void RefreshGroupedCaches()
        {
            _cachedSeasonal.Clear();
            foreach (var currencyId in _allSeasonalTokens)
            {
                _cachedCurrencies.TryGetValue(currencyId, out int value);
                _cachedSeasonal[currencyId] = value;
            }
            _cachedKeys.Clear();
            foreach (var currencyId in _allKeys)
            {
                _cachedCurrencies.TryGetValue(currencyId, out int value);
                _cachedKeys[currencyId] = value;
            }
            _cachedSpecial.Clear();
            foreach (var currencyId in _allSpecialCurrencies)
            {
                _cachedCurrencies.TryGetValue(currencyId, out int value);
                _cachedSpecial[currencyId] = value;
            }
        }

        private void OnGUI()
        {
            // Don't show HUD until vault is loaded for the current character
            if (!_isEnabled || _vaultManager == null || !PlayerPatches.IsVaultLoaded) return;

            // Don't show HUD if main vault window is open
            var vaultUI = Plugin.GetVaultUI();
            if (vaultUI != null && vaultUI.IsVisible) return;

            InitializeStyles();

            // Ensure grouped caches are populated on first show (before first Update interval)
            if (_cachedSeasonal.Count == 0 && _cachedCurrencies.Count > 0)
                RefreshGroupedCaches();

            // Calculate HUD width based on content
            float hudWidth = CalculateHUDWidth();

            // Ensure minimum width so HUD always shows
            hudWidth = Mathf.Max(hudWidth, MIN_HUD_WIDTH);

            // Get position
            Rect hudRect = GetHUDRect(hudWidth);

            // Draw background
            GUI.Box(hudRect, "", _hudBackgroundStyle);

            // Draw content - vertically centered
            float padding = BASE_PADDING * _scale;
            GUILayout.BeginArea(new Rect(hudRect.x + padding, hudRect.y + 3, hudRect.width - padding * 2, hudRect.height - 6));
            GUILayout.BeginHorizontal();

            // All groups are always shown (use cached dicts to avoid per-frame allocations)
            DrawCurrencyItems(_cachedSeasonal);
            DrawSeparator();
            DrawCurrencyItems(_cachedKeys);
            DrawSeparator();
            DrawCurrencyItems(_cachedSpecial);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawSeparator()
        {
            float groupSpacing = BASE_GROUP_SPACING * _scale;
            float iconSize = BASE_HUD_ICON_SIZE * _scale;

            GUILayout.Space(groupSpacing / 2 - 1);
            GUILayout.Box("", GUIStyle.none, GUILayout.Width(1), GUILayout.Height(iconSize));
            // Draw a vertical line
            Rect lastRect = GUILayoutUtility.GetLastRect();
            GUI.DrawTexture(new Rect(lastRect.x, lastRect.y + 2, 1, iconSize - 4), _separatorTexture);
            GUILayout.Space(groupSpacing / 2 - 1);
        }

        private float CalculateHUDWidth()
        {
            float padding = BASE_PADDING * _scale;
            float groupSpacing = BASE_GROUP_SPACING * _scale;

            float width = padding * 2;

            // Use cached dicts to avoid allocations
            width += CalculateGroupWidth(_cachedSeasonal);
            width += CalculateGroupWidth(_cachedKeys);
            width += CalculateGroupWidth(_cachedSpecial);

            // Add separator spacing between all 3 groups
            width += 2 * groupSpacing;

            // Add extra padding to prevent cutoff on the right side
            width += 24;

            return width;
        }

        // Icon size for HUD display (scaled by _scale)
        private const float BASE_HUD_ICON_SIZE = 18f;

        private float CalculateGroupWidth(Dictionary<string, int> items)
        {
            if (items.Count == 0) return 0;

            float itemSpacing = BASE_ITEM_SPACING * _scale;
            float iconSize = BASE_HUD_ICON_SIZE * _scale;

            float width = 0;
            foreach (var kvp in items)
            {
                string value = FormatNumber(kvp.Value);
                // Icon + small gap + value text + item spacing
                // Use ~9px per character at scale 1, scaled with _scale
                width += iconSize + 3 * _scale + value.Length * 9 * _scale + itemSpacing;
            }

            // Remove trailing item spacing
            if (items.Count > 0)
                width -= itemSpacing;

            return width;
        }

        private Rect GetHUDRect(float width)
        {
            float hudHeight = BASE_HUD_HEIGHT * _scale;

            float x = 0, y = 0;

            switch (_position)
            {
                case HUDPosition.TopLeft:
                    x = 0;
                    y = 0;
                    break;
                case HUDPosition.TopCenter:
                    x = (Screen.width - width) / 2;
                    y = 0;
                    break;
                case HUDPosition.TopRight:
                    x = Screen.width - width;
                    y = 0;
                    break;
                case HUDPosition.BottomLeft:
                    x = 0;
                    y = Screen.height - hudHeight;
                    break;
                case HUDPosition.BottomCenter:
                    x = (Screen.width - width) / 2;
                    y = Screen.height - hudHeight;
                    break;
                case HUDPosition.BottomRight:
                    x = Screen.width - width;
                    y = Screen.height - hudHeight;
                    break;
            }

            return new Rect(x, y, width, hudHeight);
        }

        private void DrawCurrencyItems(Dictionary<string, int> items)
        {
            if (items.Count == 0) return;

            float itemSpacing = BASE_ITEM_SPACING * _scale;
            float iconSize = BASE_HUD_ICON_SIZE * _scale;

            bool first = true;
            foreach (var kvp in items)
            {
                if (!first)
                    GUILayout.Space(itemSpacing);
                first = false;

                // Draw icon if available
                Texture2D iconTexture = IconCache.GetIconForCurrency(kvp.Key);
                if (iconTexture != null && IconCache.IsIconLoaded(kvp.Key))
                {
                    // Draw the actual game icon
                    GUILayout.Box(iconTexture, GUIStyle.none, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                }
                else
                {
                    // Fallback to short text name while loading
                    string shortName = GetShortName(kvp.Key);
                    GUILayout.Label(shortName, _currencyLabelStyle);
                }

                // Draw the value right next to the icon (formatted with K/M)
                GUILayout.Label(FormatNumber(kvp.Value), _currencyValueStyle);
            }
        }

        // All seasonal token IDs that should always be shown
        private static readonly string[] _allSeasonalTokens = new[]
        {
            "seasonal_Spring",
            "seasonal_Summer",
            "seasonal_Fall",
            "seasonal_Winter"
        };

        // All key IDs that should always be shown
        private static readonly string[] _allKeys = new[]
        {
            "key_copper",
            "key_iron",
            "key_adamant",
            "key_mithril",
            "key_sunite",
            "key_glorite",
            "key_kingslostmine"
        };

        // All special currency IDs that should always be shown
        private static readonly string[] _allSpecialCurrencies = new[]
        {
            "special_communitytoken",
            "special_doubloon",
            "special_blackbottlecap",
            "special_redcarnivalticket",
            "special_candycornpieces",
            "special_manashard"
        };

        /// <summary>
        /// Format a number with K/M suffixes for compact display.
        /// 1000 -> 1K, 1500 -> 1.5K, 1000000 -> 1M
        /// </summary>
        private string FormatNumber(int value)
        {
            if (value >= 1000000)
            {
                float millions = value / 1000000f;
                if (millions >= 10f || millions == (int)millions)
                    return $"{(int)millions}M";
                return $"{millions:0.#}M";
            }
            else if (value >= 1000)
            {
                float thousands = value / 1000f;
                if (thousands >= 10f || thousands == (int)thousands)
                    return $"{(int)thousands}K";
                return $"{thousands:0.#}K";
            }
            return value.ToString();
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
