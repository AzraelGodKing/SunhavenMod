using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace SunHavenMuseumUtilityTracker.UI
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

        // Track if initialized
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the icon cache - creates fallback texture only.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            Plugin.Log?.LogInfo("[IconCache] Initializing icon cache...");

            // Create fallback texture
            _fallbackTexture = CreateFallbackTexture();
            Plugin.Log?.LogInfo("[IconCache] Created fallback texture");
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
                    "Database",
                    "Wish.Database",
                    "PSS.Database",
                    "SunHaven.Database",
                };

                foreach (var typeName in databaseTypeNames)
                {
                    _databaseType = AccessTools.TypeByName(typeName);
                    if (_databaseType != null)
                    {
                        Plugin.Log?.LogInfo($"[IconCache] Found Database type: {_databaseType.FullName}");
                        break;
                    }
                }

                // If still not found, search all loaded assemblies
                if (_databaseType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            foreach (var type in assembly.GetTypes())
                            {
                                if (type.Name == "Database" && !type.IsNested)
                                {
                                    var typeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                                    foreach (var m in typeMethods)
                                    {
                                        if (m.Name == "GetData" && m.IsGenericMethod)
                                        {
                                            _databaseType = type;
                                            Plugin.Log?.LogInfo($"[IconCache] Found Database type: {type.FullName}");
                                            break;
                                        }
                                    }
                                    if (_databaseType != null) break;
                                }
                            }
                            if (_databaseType != null) break;
                        }
                        catch { }
                    }
                }

                if (_databaseType == null)
                {
                    Plugin.Log?.LogError("[IconCache] Could not find Database type");
                    return false;
                }

                // Find ItemData type
                _itemDataType = AccessTools.TypeByName("Wish.ItemData");
                if (_itemDataType == null)
                {
                    Plugin.Log?.LogError("[IconCache] Could not find Wish.ItemData type");
                    return false;
                }

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
                            Plugin.Log?.LogInfo("[IconCache] Found Database.GetData method");
                            break;
                        }
                    }
                }

                if (_getDataMethod == null)
                {
                    Plugin.Log?.LogError("[IconCache] Could not find Database.GetData method");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"[IconCache] Error initializing reflection: {ex.Message}");
                return false;
            }
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
        /// Load an icon from the game's database using reflection.
        /// </summary>
        private static void LoadIcon(int itemId)
        {
            if (_loadingItems.Contains(itemId) || _iconCache.ContainsKey(itemId))
            {
                return;
            }

            _loadingItems.Add(itemId);

            try
            {
                if (!InitializeReflection() || _getDataMethod == null)
                {
                    _failedItems.Add(itemId);
                    _loadingItems.Remove(itemId);
                    return;
                }

                // Create the success callback using Expression trees
                var callbackDelegateType = typeof(Action<>).MakeGenericType(_itemDataType);
                var itemDataParam = Expression.Parameter(_itemDataType, "itemData");
                var itemIdConst = Expression.Constant(itemId);
                var onLoadedMethod = typeof(IconCache).GetMethod(nameof(OnIconLoadedInternal), BindingFlags.NonPublic | BindingFlags.Static);
                var callExpr = Expression.Call(onLoadedMethod, itemIdConst, Expression.Convert(itemDataParam, typeof(object)));
                var successCallback = Expression.Lambda(callbackDelegateType, callExpr, itemDataParam).Compile();

                // Create the fail callback
                var onFailedMethod = typeof(IconCache).GetMethod(nameof(OnIconLoadFailed), BindingFlags.NonPublic | BindingFlags.Static);
                var failCallExpr = Expression.Call(onFailedMethod, itemIdConst);
                var failCallback = Expression.Lambda<Action>(failCallExpr).Compile();

                // Invoke Database.GetData<ItemData>(itemId, successCallback, failCallback)
                _getDataMethod.Invoke(null, new object[] { itemId, successCallback, failCallback });
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[IconCache] Error loading icon {itemId}: {ex.Message}");
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

            if (itemData == null)
            {
                _failedItems.Add(itemId);
                return;
            }

            try
            {
                Type actualType = itemData.GetType();
                object spriteObj = null;

                // Try finding "icon" with various binding flags
                var bindingFlagSets = new[]
                {
                    BindingFlags.Public | BindingFlags.Instance,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                };

                // Try property first
                foreach (var flags in bindingFlagSets)
                {
                    var iconProp = actualType.GetProperty("icon", flags);
                    if (iconProp != null)
                    {
                        spriteObj = iconProp.GetValue(itemData);
                        break;
                    }
                }

                // Try field if property not found
                if (spriteObj == null)
                {
                    foreach (var flags in bindingFlagSets)
                    {
                        var iconField = actualType.GetField("icon", flags);
                        if (iconField != null)
                        {
                            spriteObj = iconField.GetValue(itemData);
                            break;
                        }
                    }
                }

                // Walk up inheritance hierarchy if still not found
                if (spriteObj == null)
                {
                    Type currentType = actualType;
                    while (currentType != null && spriteObj == null)
                    {
                        var props = currentType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        foreach (var prop in props)
                        {
                            if (prop.Name.Equals("icon", StringComparison.OrdinalIgnoreCase))
                            {
                                spriteObj = prop.GetValue(itemData);
                                break;
                            }
                        }

                        if (spriteObj == null)
                        {
                            var fields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                            foreach (var field in fields)
                            {
                                if (field.Name.Equals("icon", StringComparison.OrdinalIgnoreCase))
                                {
                                    spriteObj = field.GetValue(itemData);
                                    break;
                                }
                            }
                        }

                        currentType = currentType.BaseType;
                    }
                }

                if (spriteObj is Sprite sprite)
                {
                    CacheSprite(itemId, sprite);
                }
                else
                {
                    _failedItems.Add(itemId);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[IconCache] Error processing icon {itemId}: {ex.Message}");
                _failedItems.Add(itemId);
            }
        }

        /// <summary>
        /// Internal callback when icon loading fails.
        /// </summary>
        private static void OnIconLoadFailed(int itemId)
        {
            _loadingItems.Remove(itemId);
            _failedItems.Add(itemId);
        }

        /// <summary>
        /// Cache a sprite by converting it to a Texture2D.
        /// </summary>
        private static void CacheSprite(int itemId, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                _failedItems.Add(itemId);
                return;
            }

            try
            {
                Texture2D texture;

                if (sprite.rect.width == sprite.texture.width && sprite.rect.height == sprite.texture.height)
                {
                    // Sprite uses the full texture
                    texture = sprite.texture;
                }
                else
                {
                    // Sprite is a sub-region, extract it
                    texture = ExtractSpriteTexture(sprite);
                }

                if (texture != null)
                {
                    _iconCache[itemId] = texture;
                }
                else
                {
                    _failedItems.Add(itemId);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogDebug($"[IconCache] Error caching sprite {itemId}: {ex.Message}");
                _failedItems.Add(itemId);
            }
        }

        /// <summary>
        /// Extract a sprite's region from its atlas texture.
        /// </summary>
        private static Texture2D ExtractSpriteTexture(Sprite sprite)
        {
            try
            {
                Rect rect = sprite.rect;
                int width = (int)rect.width;
                int height = (int)rect.height;

                if (!sprite.texture.isReadable)
                {
                    return CopyTextureViaRenderTexture(sprite);
                }

                Texture2D extracted = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Color[] pixels = sprite.texture.GetPixels((int)rect.x, (int)rect.y, width, height);
                extracted.SetPixels(pixels);
                extracted.Apply();

                return extracted;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Copy a sprite's texture using RenderTexture for non-readable textures.
        /// </summary>
        private static Texture2D CopyTextureViaRenderTexture(Sprite sprite)
        {
            try
            {
                Rect rect = sprite.rect;
                int width = (int)rect.width;
                int height = (int)rect.height;

                RenderTexture rt = RenderTexture.GetTemporary(
                    sprite.texture.width,
                    sprite.texture.height,
                    0,
                    RenderTextureFormat.ARGB32
                );

                Graphics.Blit(sprite.texture, rt);

                RenderTexture previousRT = RenderTexture.active;
                RenderTexture.active = rt;

                Texture2D extracted = new Texture2D(width, height, TextureFormat.RGBA32, false);
                extracted.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
                extracted.Apply();

                RenderTexture.active = previousRT;
                RenderTexture.ReleaseTemporary(rt);

                return extracted;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Create a fallback texture for items without icons.
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
        /// Clear the icon cache.
        /// </summary>
        public static void Clear()
        {
            _iconCache.Clear();
            _loadingItems.Clear();
            _failedItems.Clear();
        }

        /// <summary>
        /// Get cache statistics.
        /// </summary>
        public static (int loaded, int loading, int failed) GetStats()
        {
            return (_iconCache.Count, _loadingItems.Count, _failedItems.Count);
        }
    }
}
