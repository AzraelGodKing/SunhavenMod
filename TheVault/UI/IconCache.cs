using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace TheVault.UI
{
    /// <summary>
    /// Caches item icons from the game's Database for use in IMGUI.
    /// Loads icons asynchronously and converts Sprites to Texture2D for IMGUI compatibility.
    /// </summary>
    public static class IconCache
    {
        // Cache of loaded textures keyed by item ID
        private static readonly Dictionary<int, Texture2D> _iconCache = new Dictionary<int, Texture2D>();

        // Track which items are currently being loaded to avoid duplicate requests
        private static readonly HashSet<int> _loadingItems = new HashSet<int>();

        // Track which items failed to load
        private static readonly HashSet<int> _failedItems = new HashSet<int>();

        // Default fallback texture (generated once)
        private static Texture2D _fallbackTexture;

        // Cached reflection types
        private static Type _databaseType;
        private static Type _itemDataType;
        private static MethodInfo _getDataMethod;
        private static bool _reflectionInitialized;

        // Item ID to currency ID mapping for all supported currencies
        private static readonly Dictionary<string, int> _currencyToItemId = new Dictionary<string, int>
        {
            // Seasonal Tokens
            { "seasonal_Spring", 18020 },
            { "seasonal_Summer", 18021 },
            { "seasonal_Fall", 18023 },
            { "seasonal_Winter", 18022 },

            // Keys
            { "key_copper", 1251 },
            { "key_iron", 1252 },
            { "key_adamant", 1253 },
            { "key_mithril", 1254 },
            { "key_sunite", 1255 },
            { "key_glorite", 1256 },
            { "key_kingslostmine", 1257 },

            // Special currencies
            { "special_communitytoken", 18013 },
            { "special_doubloon", 60014 },
            { "special_blackbottlecap", 60013 },
            { "special_redcarnivalticket", 18012 },
            { "special_candycornpieces", 18016 },
            { "special_manashard", 18015 }
        };

        // Track if icons have been loaded
        private static bool _iconsLoaded = false;

        /// <summary>
        /// Initialize the icon cache - creates fallback texture only.
        /// Call this during plugin initialization.
        /// Actual icon loading is deferred until LoadAllIcons() is called.
        /// </summary>
        public static void Initialize()
        {
            Plugin.Log?.LogInfo("[IconCache] ========== INITIALIZING ICON CACHE ==========");

            // Create fallback texture
            _fallbackTexture = CreateFallbackTexture();
            Plugin.Log?.LogInfo("[IconCache] Created fallback texture");

            // Log all currencies we're going to load (but don't load them yet)
            Plugin.Log?.LogInfo("[IconCache] Currency to ItemID mappings (loading deferred until game is ready):");
            foreach (var kvp in _currencyToItemId)
            {
                Plugin.Log?.LogInfo($"[IconCache]   {kvp.Key} -> ItemID {kvp.Value}");
            }

            Plugin.Log?.LogInfo("[IconCache] ========== ICON CACHE INITIALIZED (icons will load when game is ready) ==========");
        }

        /// <summary>
        /// Initialize reflection types for Database access.
        /// </summary>
        private static bool InitializeReflection()
        {
            if (_reflectionInitialized) return _databaseType != null && _itemDataType != null && _getDataMethod != null;

            _reflectionInitialized = true;

            try
            {
                // Find Database type - try multiple namespaces
                string[] databaseTypeNames = new[]
                {
                    "Database",           // No namespace
                    "Wish.Database",      // Wish namespace
                    "PSS.Database",       // PSS namespace (from PSS.Database.dll)
                    "SunHaven.Database",  // SunHaven namespace
                };

                foreach (var typeName in databaseTypeNames)
                {
                    _databaseType = AccessTools.TypeByName(typeName);
                    if (_databaseType != null)
                    {
                        Plugin.Log?.LogInfo($"[IconCache] Found Database type: {_databaseType.FullName} (searched: {typeName})");
                        break;
                    }
                }

                // If still not found, search all loaded assemblies
                if (_databaseType == null)
                {
                    Plugin.Log?.LogInfo("[IconCache] Database type not found by name, searching all loaded assemblies...");
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            foreach (var type in assembly.GetTypes())
                            {
                                if (type.Name == "Database" && !type.IsNested)
                                {
                                    // Check if it has a GetData method
                                    var typeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                                    foreach (var m in typeMethods)
                                    {
                                        if (m.Name == "GetData" && m.IsGenericMethod)
                                        {
                                            _databaseType = type;
                                            Plugin.Log?.LogInfo($"[IconCache] Found Database type by search: {type.FullName} in {assembly.GetName().Name}");
                                            break;
                                        }
                                    }
                                    if (_databaseType != null) break;
                                }
                            }
                            if (_databaseType != null) break;
                        }
                        catch
                        {
                            // Some assemblies may throw on GetTypes(), skip them
                        }
                    }
                }

                if (_databaseType == null)
                {
                    Plugin.Log?.LogError("[IconCache] FAILED: Could not find Database type in any assembly");
                    return false;
                }

                // Find ItemData type
                _itemDataType = AccessTools.TypeByName("Wish.ItemData");
                if (_itemDataType == null)
                {
                    Plugin.Log?.LogError("[IconCache] FAILED: Could not find Wish.ItemData type");
                    return false;
                }
                Plugin.Log?.LogInfo($"[IconCache] Found ItemData type: {_itemDataType.FullName}");

                // Find the generic GetData method
                var methods = _databaseType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (var method in methods)
                {
                    if (method.Name == "GetData" && method.IsGenericMethod)
                    {
                        var genericParams = method.GetGenericArguments();
                        var methodParams = method.GetParameters();

                        // We want GetData<T>(int id, Action<T> callback, Action failCallback)
                        if (genericParams.Length == 1 && methodParams.Length == 3 &&
                            methodParams[0].ParameterType == typeof(int))
                        {
                            _getDataMethod = method.MakeGenericMethod(_itemDataType);
                            Plugin.Log?.LogInfo($"[IconCache] Found GetData method with signature: GetData<ItemData>(int, Action<ItemData>, Action)");
                            break;
                        }
                    }
                }

                if (_getDataMethod == null)
                {
                    Plugin.Log?.LogError("[IconCache] FAILED: Could not find Database.GetData<T> method");

                    // Log available methods for debugging
                    Plugin.Log?.LogInfo("[IconCache] Available Database methods:");
                    foreach (var m in methods)
                    {
                        var ps = m.GetParameters();
                        var paramStr = string.Join(", ", Array.ConvertAll(ps, p => $"{p.ParameterType.Name} {p.Name}"));
                        Plugin.Log?.LogInfo($"[IconCache]   {m.Name}({paramStr}) - Generic: {m.IsGenericMethod}");
                    }
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[IconCache] EXCEPTION initializing reflection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load all currency icons from the game's Database.
        /// Call this AFTER the game is fully loaded (e.g., when player initializes).
        /// </summary>
        public static void LoadAllIcons()
        {
            if (_iconsLoaded)
            {
                Plugin.Log?.LogInfo("[IconCache] Icons already loaded, skipping");
                return;
            }

            Plugin.Log?.LogInfo("[IconCache] ========== LOADING ALL ICONS ==========");

            // Initialize reflection types
            if (!InitializeReflection())
            {
                Plugin.Log?.LogError("[IconCache] Failed to initialize reflection, cannot load icons");
                _iconsLoaded = true; // Prevent repeated attempts
                return;
            }

            // Preload all currency icons
            foreach (var kvp in _currencyToItemId)
            {
                Plugin.Log?.LogInfo($"[IconCache] Queuing load for: {kvp.Key} (ItemID: {kvp.Value})");
                LoadIcon(kvp.Value);
            }

            _iconsLoaded = true;
            Plugin.Log?.LogInfo($"[IconCache] ========== QUEUED {_currencyToItemId.Count} ICONS ==========");
        }

        /// <summary>
        /// Get the icon texture for a currency ID.
        /// Returns the cached texture if available, or a fallback texture while loading.
        /// </summary>
        public static Texture2D GetIconForCurrency(string currencyId)
        {
            if (_currencyToItemId.TryGetValue(currencyId, out int itemId))
            {
                return GetIcon(itemId);
            }
            return _fallbackTexture;
        }

        /// <summary>
        /// Get the icon texture for an item ID.
        /// Returns the cached texture if available, or a fallback texture while loading.
        /// </summary>
        public static Texture2D GetIcon(int itemId)
        {
            // Return cached texture if available
            if (_iconCache.TryGetValue(itemId, out Texture2D cached))
            {
                return cached;
            }

            // Start loading if not already loading or failed
            if (!_loadingItems.Contains(itemId) && !_failedItems.Contains(itemId))
            {
                LoadIcon(itemId);
            }

            return _fallbackTexture;
        }

        /// <summary>
        /// Check if an icon is loaded and ready to use.
        /// </summary>
        public static bool IsIconLoaded(int itemId)
        {
            return _iconCache.ContainsKey(itemId);
        }

        /// <summary>
        /// Check if an icon is loaded for a currency ID.
        /// </summary>
        public static bool IsIconLoaded(string currencyId)
        {
            if (_currencyToItemId.TryGetValue(currencyId, out int itemId))
            {
                return IsIconLoaded(itemId);
            }
            return false;
        }

        /// <summary>
        /// Get the item ID for a currency ID.
        /// </summary>
        public static int GetItemIdForCurrency(string currencyId)
        {
            return _currencyToItemId.TryGetValue(currencyId, out int itemId) ? itemId : -1;
        }

        /// <summary>
        /// Load an icon from the game's database using reflection.
        /// </summary>
        private static void LoadIcon(int itemId)
        {
            if (_loadingItems.Contains(itemId))
            {
                return;
            }
            if (_iconCache.ContainsKey(itemId))
            {
                return;
            }

            _loadingItems.Add(itemId);
            Plugin.Log?.LogInfo($"[IconCache] Loading icon for item {itemId}...");

            try
            {
                if (_getDataMethod == null)
                {
                    Plugin.Log?.LogError($"[IconCache] GetData method not available");
                    _failedItems.Add(itemId);
                    _loadingItems.Remove(itemId);
                    return;
                }

                // Create the success callback using Expression trees
                // This creates: (ItemData itemData) => OnIconLoadedInternal(itemId, itemData)
                var callbackDelegateType = typeof(Action<>).MakeGenericType(_itemDataType);
                var itemDataParam = Expression.Parameter(_itemDataType, "itemData");
                var itemIdConst = Expression.Constant(itemId);
                var onLoadedMethod = typeof(IconCache).GetMethod(nameof(OnIconLoadedInternal), BindingFlags.NonPublic | BindingFlags.Static);
                var callExpr = Expression.Call(onLoadedMethod, itemIdConst, Expression.Convert(itemDataParam, typeof(object)));
                var successCallback = Expression.Lambda(callbackDelegateType, callExpr, itemDataParam).Compile();

                // Create the fail callback: () => OnIconLoadFailed(itemId)
                var onFailedMethod = typeof(IconCache).GetMethod(nameof(OnIconLoadFailed), BindingFlags.NonPublic | BindingFlags.Static);
                var failCallExpr = Expression.Call(onFailedMethod, itemIdConst);
                var failCallback = Expression.Lambda<Action>(failCallExpr).Compile();

                // Invoke Database.GetData<ItemData>(itemId, successCallback, failCallback)
                _getDataMethod.Invoke(null, new object[] { itemId, successCallback, failCallback });
                Plugin.Log?.LogInfo($"[IconCache] Database.GetData invoked for item {itemId}");
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[IconCache] EXCEPTION loading icon for item {itemId}: {ex.Message}");
                Plugin.Log?.LogError($"[IconCache] Stack trace: {ex.StackTrace}");
                _failedItems.Add(itemId);
                _loadingItems.Remove(itemId);
            }
        }

        /// <summary>
        /// Internal callback when an icon is loaded from the database.
        /// </summary>
        private static void OnIconLoadedInternal(int itemId, object itemData)
        {
            _loadingItems.Remove(itemId);
            Plugin.Log?.LogInfo($"[IconCache] OnIconLoaded callback for item {itemId}");

            if (itemData == null)
            {
                Plugin.Log?.LogWarning($"[IconCache] FAILED: ItemData is null for item {itemId}");
                _failedItems.Add(itemId);
                return;
            }

            try
            {
                // Get the actual runtime type of the item data
                Type actualType = itemData.GetType();
                Plugin.Log?.LogInfo($"[IconCache] ItemData actual type: {actualType.FullName}");

                // Get the name and id for logging - try multiple approaches
                string name = "unknown";
                int id = -1;

                // Try property first, then field for name
                var nameProp = actualType.GetProperty("name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (nameProp != null)
                    name = nameProp.GetValue(itemData) as string ?? "unknown";
                else
                {
                    var nameField = actualType.GetField("name", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (nameField != null)
                        name = nameField.GetValue(itemData) as string ?? "unknown";
                }

                // Try property first, then field for id
                var idProp = actualType.GetProperty("id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (idProp != null)
                    id = (int)idProp.GetValue(itemData);
                else
                {
                    var idField = actualType.GetField("id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (idField != null)
                        id = (int)idField.GetValue(itemData);
                }

                Plugin.Log?.LogInfo($"[IconCache] ItemData received: {name} (id: {id})");

                // Try to get the icon using multiple binding flag combinations
                object spriteObj = null;
                string foundVia = null;

                // Binding flag combinations to try
                var bindingFlagSets = new[]
                {
                    BindingFlags.Public | BindingFlags.Instance,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                };

                // Try finding "icon" as property
                foreach (var flags in bindingFlagSets)
                {
                    var iconProp = actualType.GetProperty("icon", flags);
                    if (iconProp != null)
                    {
                        spriteObj = iconProp.GetValue(itemData);
                        foundVia = $"property with flags {flags}";
                        break;
                    }
                }

                // Try finding "icon" as field if property not found
                if (spriteObj == null)
                {
                    foreach (var flags in bindingFlagSets)
                    {
                        var iconField = actualType.GetField("icon", flags);
                        if (iconField != null)
                        {
                            spriteObj = iconField.GetValue(itemData);
                            foundVia = $"field with flags {flags}";
                            break;
                        }
                    }
                }

                // Also try walking up the inheritance hierarchy manually
                if (spriteObj == null)
                {
                    Type currentType = actualType;
                    while (currentType != null && spriteObj == null)
                    {
                        Plugin.Log?.LogInfo($"[IconCache] Checking type: {currentType.FullName}");

                        // Check properties
                        var props = currentType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        foreach (var prop in props)
                        {
                            if (prop.Name.Equals("icon", StringComparison.OrdinalIgnoreCase))
                            {
                                spriteObj = prop.GetValue(itemData);
                                foundVia = $"property '{prop.Name}' on {currentType.Name}";
                                break;
                            }
                        }

                        if (spriteObj == null)
                        {
                            // Check fields
                            var fields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                            foreach (var field in fields)
                            {
                                if (field.Name.Equals("icon", StringComparison.OrdinalIgnoreCase))
                                {
                                    spriteObj = field.GetValue(itemData);
                                    foundVia = $"field '{field.Name}' on {currentType.Name}";
                                    break;
                                }
                            }
                        }

                        currentType = currentType.BaseType;
                    }
                }

                if (spriteObj != null)
                {
                    Plugin.Log?.LogInfo($"[IconCache] Found icon via {foundVia}");
                }
                else
                {
                    // Still not found - log ALL members for debugging
                    Plugin.Log?.LogWarning($"[IconCache] FAILED: Could not find 'icon' member. Dumping all members:");

                    Type dumpType = actualType;
                    while (dumpType != null)
                    {
                        Plugin.Log?.LogInfo($"[IconCache] === Type: {dumpType.FullName} ===");

                        var props = dumpType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        foreach (var prop in props)
                        {
                            Plugin.Log?.LogInfo($"[IconCache]   Property: {prop.Name} ({prop.PropertyType.Name})");
                        }

                        var fields = dumpType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        foreach (var field in fields)
                        {
                            Plugin.Log?.LogInfo($"[IconCache]   Field: {field.Name} ({field.FieldType.Name})");
                        }

                        dumpType = dumpType.BaseType;
                        if (dumpType == typeof(object)) break; // Stop at System.Object
                    }

                    _failedItems.Add(itemId);
                    return;
                }

                if (spriteObj is Sprite sprite)
                {
                    Plugin.Log?.LogInfo($"[IconCache] Got Sprite for item {itemId}: {sprite.name}");
                    CacheSprite(itemId, sprite);
                }
                else
                {
                    Plugin.Log?.LogWarning($"[IconCache] FAILED: Icon is not a Sprite for item {itemId}, got: {spriteObj?.GetType().Name ?? "null"}");
                    _failedItems.Add(itemId);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[IconCache] EXCEPTION processing icon for item {itemId}: {ex.Message}");
                Plugin.Log?.LogError($"[IconCache] Stack trace: {ex.StackTrace}");
                _failedItems.Add(itemId);
            }
        }

        /// <summary>
        /// Internal callback when icon loading fails.
        /// </summary>
        private static void OnIconLoadFailed(int itemId)
        {
            Plugin.Log?.LogWarning($"[IconCache] Database.GetData FAILED for item {itemId} (fail callback invoked)");
            _loadingItems.Remove(itemId);
            _failedItems.Add(itemId);
        }

        /// <summary>
        /// Cache a sprite by converting it to a Texture2D.
        /// </summary>
        private static void CacheSprite(int itemId, Sprite sprite)
        {
            if (sprite == null)
            {
                Plugin.Log?.LogWarning($"[IconCache] FAILED: Sprite is null for item {itemId}");
                _failedItems.Add(itemId);
                return;
            }

            if (sprite.texture == null)
            {
                Plugin.Log?.LogWarning($"[IconCache] FAILED: Sprite texture is null for item {itemId}");
                _failedItems.Add(itemId);
                return;
            }

            Plugin.Log?.LogInfo($"[IconCache] Caching sprite for item {itemId}:");
            Plugin.Log?.LogInfo($"[IconCache]   Sprite name: {sprite.name}");
            Plugin.Log?.LogInfo($"[IconCache]   Sprite rect: {sprite.rect}");
            Plugin.Log?.LogInfo($"[IconCache]   Texture size: {sprite.texture.width}x{sprite.texture.height}");
            Plugin.Log?.LogInfo($"[IconCache]   Texture readable: {sprite.texture.isReadable}");

            try
            {
                // For IMGUI, we need to extract the sprite region
                Texture2D texture;

                if (sprite.rect.width == sprite.texture.width && sprite.rect.height == sprite.texture.height)
                {
                    // Sprite uses the full texture, use it directly
                    Plugin.Log?.LogInfo($"[IconCache] Using full texture directly");
                    texture = sprite.texture;
                }
                else
                {
                    // Sprite is a sub-region, need to extract it
                    Plugin.Log?.LogInfo($"[IconCache] Sprite is sub-region, extracting...");
                    texture = ExtractSpriteTexture(sprite);
                }

                if (texture != null)
                {
                    _iconCache[itemId] = texture;
                    var stats = GetStats();
                    Plugin.Log?.LogInfo($"[IconCache] SUCCESS: Cached icon for item {itemId} (Total cached: {stats.loaded}, Loading: {stats.loading}, Failed: {stats.failed})");
                }
                else
                {
                    Plugin.Log?.LogWarning($"[IconCache] FAILED: Extracted texture is null for item {itemId}");
                    _failedItems.Add(itemId);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[IconCache] EXCEPTION caching sprite for item {itemId}: {ex.Message}");
                _failedItems.Add(itemId);
            }
        }

        /// <summary>
        /// Extract a sprite's region from its atlas texture into a new Texture2D.
        /// </summary>
        private static Texture2D ExtractSpriteTexture(Sprite sprite)
        {
            try
            {
                Rect rect = sprite.rect;
                int width = (int)rect.width;
                int height = (int)rect.height;

                // Check if the source texture is readable
                if (!sprite.texture.isReadable)
                {
                    // If not readable, we need to use a RenderTexture workaround
                    return CopyTextureViaRenderTexture(sprite);
                }

                // Create a new texture for the extracted region
                Texture2D extracted = new Texture2D(width, height, TextureFormat.RGBA32, false);

                // Get the pixels from the sprite's region
                Color[] pixels = sprite.texture.GetPixels(
                    (int)rect.x,
                    (int)rect.y,
                    width,
                    height
                );

                extracted.SetPixels(pixels);
                extracted.Apply();

                return extracted;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[IconCache] Error extracting sprite texture: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Copy a sprite's texture using a RenderTexture (for non-readable textures).
        /// </summary>
        private static Texture2D CopyTextureViaRenderTexture(Sprite sprite)
        {
            try
            {
                Rect rect = sprite.rect;
                int width = (int)rect.width;
                int height = (int)rect.height;

                // Create temporary RenderTexture
                RenderTexture rt = RenderTexture.GetTemporary(
                    sprite.texture.width,
                    sprite.texture.height,
                    0,
                    RenderTextureFormat.ARGB32
                );

                // Copy the texture to the RenderTexture
                Graphics.Blit(sprite.texture, rt);

                // Store the active RenderTexture
                RenderTexture previousRT = RenderTexture.active;
                RenderTexture.active = rt;

                // Create new texture and read pixels
                Texture2D extracted = new Texture2D(width, height, TextureFormat.RGBA32, false);
                extracted.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
                extracted.Apply();

                // Restore previous RenderTexture
                RenderTexture.active = previousRT;
                RenderTexture.ReleaseTemporary(rt);

                return extracted;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[IconCache] Error copying texture via RenderTexture: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a simple fallback texture to display while icons are loading.
        /// </summary>
        private static Texture2D CreateFallbackTexture()
        {
            int size = 32;
            Texture2D tex = new Texture2D(size, size);
            Color bgColor = new Color(0.3f, 0.3f, 0.4f, 0.8f);
            Color borderColor = new Color(0.5f, 0.5f, 0.6f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Simple border effect
                    if (x == 0 || x == size - 1 || y == 0 || y == size - 1)
                        tex.SetPixel(x, y, borderColor);
                    else
                        tex.SetPixel(x, y, bgColor);
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Clear the icon cache (useful for reload/cleanup).
        /// </summary>
        public static void Clear()
        {
            _iconCache.Clear();
            _loadingItems.Clear();
            _failedItems.Clear();
            _iconsLoaded = false;
        }

        /// <summary>
        /// Get statistics about the icon cache.
        /// </summary>
        public static (int loaded, int loading, int failed) GetStats()
        {
            return (_iconCache.Count, _loadingItems.Count, _failedItems.Count);
        }

        /// <summary>
        /// Log the current status of all icons.
        /// </summary>
        public static void LogStatus()
        {
            var stats = GetStats();
            Plugin.Log?.LogInfo($"[IconCache] ========== ICON CACHE STATUS ==========");
            Plugin.Log?.LogInfo($"[IconCache] Loaded: {stats.loaded}, Loading: {stats.loading}, Failed: {stats.failed}");

            Plugin.Log?.LogInfo($"[IconCache] Loaded icons:");
            foreach (var kvp in _iconCache)
            {
                Plugin.Log?.LogInfo($"[IconCache]   ItemID {kvp.Key}: {kvp.Value.width}x{kvp.Value.height}");
            }

            if (_loadingItems.Count > 0)
            {
                Plugin.Log?.LogInfo($"[IconCache] Still loading:");
                foreach (var itemId in _loadingItems)
                {
                    Plugin.Log?.LogInfo($"[IconCache]   ItemID {itemId}");
                }
            }

            if (_failedItems.Count > 0)
            {
                Plugin.Log?.LogInfo($"[IconCache] Failed to load:");
                foreach (var itemId in _failedItems)
                {
                    Plugin.Log?.LogInfo($"[IconCache]   ItemID {itemId}");
                }
            }

            Plugin.Log?.LogInfo($"[IconCache] ========================================");
        }
    }
}
