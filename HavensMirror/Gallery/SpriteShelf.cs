using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HavensMirror.Gallery
{
    /// <summary>
    /// Loads and caches portrait sprites from a character (or shared) gallery folder.
    /// </summary>
    public sealed class SpriteShelf : IDisposable
    {
        private readonly Dictionary<LookSlot, Sprite> _sprites = new Dictionary<LookSlot, Sprite>();
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private string _loadedFrom;

        public bool HasAny => _sprites.Count > 0;
        public string LoadedFrom => _loadedFrom;

        public void Clear()
        {
            foreach (var pair in _sprites)
            {
                if (pair.Value != null)
                    UnityEngine.Object.Destroy(pair.Value);
            }

            _sprites.Clear();

            foreach (var tex in _textures)
            {
                if (tex != null)
                    UnityEngine.Object.Destroy(tex);
            }

            _textures.Clear();
            _loadedFrom = null;
        }

        public void Dispose() => Clear();

        /// <summary>
        /// Load portraits for the active character, falling back to the shared folder.
        /// Returns a short status message for logs / notifications.
        /// </summary>
        public string LoadForCharacter(string characterName)
        {
            Clear();

            string[] candidates =
            {
                GalleryPaths.CharacterFolder(characterName),
                GalleryPaths.SharedFolder
            };

            foreach (string folder in candidates)
            {
                if (!Directory.Exists(folder))
                    continue;

                Sprite first = null;
                foreach (LookSlot slot in Enum.GetValues(typeof(LookSlot)))
                {
                    string path = Path.Combine(folder, GalleryPaths.FileNameFor(slot));
                    Sprite sprite = TryLoadPng(path);
                    if (sprite == null)
                        continue;

                    _sprites[slot] = sprite;
                    if (first == null)
                        first = sprite;
                }

                if (first == null)
                    continue;

                // Fill gaps with the first successfully loaded portrait in this folder.
                foreach (LookSlot slot in Enum.GetValues(typeof(LookSlot)))
                {
                    if (!_sprites.ContainsKey(slot))
                        _sprites[slot] = first;
                }

                _loadedFrom = folder;
                return $"Loaded {_sprites.Count} look slot(s) from '{folder}'.";
            }

            return "No portrait PNGs found (checked character folder and shared fallback).";
        }

        public Sprite Get(LookSlot slot)
        {
            return _sprites.TryGetValue(slot, out var sprite) ? sprite : null;
        }

        private Sprite TryLoadPng(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                byte[] bytes = File.ReadAllBytes(path);
                if (bytes == null || bytes.Length == 0)
                    return null;

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    UnityEngine.Object.Destroy(texture);
                    Plugin.Log?.LogWarning($"[Gallery] Failed to decode PNG: {path}");
                    return null;
                }

                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.name = Path.GetFileNameWithoutExtension(path);

                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);

                sprite.name = texture.name;
                _textures.Add(texture);
                Plugin.Log?.LogInfo($"[Gallery] Loaded {path} ({texture.width}x{texture.height}).");
                return sprite;
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogWarning($"[Gallery] Error loading '{path}': {ex.Message}");
                return null;
            }
        }
    }
}
