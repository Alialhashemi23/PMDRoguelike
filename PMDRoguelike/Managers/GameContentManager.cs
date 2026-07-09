using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace PMDRoguelike.Managers
{
    /// <summary>
    /// Central asset access. Textures are cached and addressed by logical string
    /// keys; missing content-pipeline assets fall back to a checkerboard placeholder,
    /// and solid-color placeholders can be registered at runtime until real sprites
    /// exist. Swapping placeholders for real art means loading the texture and
    /// registering it under the same key — callers never change.
    /// </summary>
    public class GameContentManager : IDisposable
    {
        private readonly ContentManager _contentManager;
        private readonly GraphicsDevice _graphicsDevice;

        private readonly Dictionary<string, Texture2D> _textures = new();
        private readonly Dictionary<string, SpriteFont> _fonts = new();
        private Texture2D _placeholder;

        private const string SpritePath = "Sprites/";
        private const string FontPath = "Fonts/";
        private const string DataPath = "Data/";

        public GameContentManager(ContentManager contentManager, GraphicsDevice graphicsDevice)
        {
            _contentManager = contentManager;
            _graphicsDevice = graphicsDevice;
        }

        /// <summary>
        /// Get a texture by logical key. Tries (in order): previously registered or
        /// cached textures, the content pipeline under Sprites/, then the placeholder.
        /// </summary>
        public Texture2D GetTexture(string key)
        {
            if (_textures.TryGetValue(key, out Texture2D cached)) return cached;

            Texture2D texture = LoadTexture(key);
            _textures[key] = texture;
            return texture;
        }

        /// <summary>Register a runtime texture (e.g. a placeholder) under a logical key.</summary>
        public void RegisterTexture(string key, Texture2D texture) => _textures[key] = texture;

        /// <summary>
        /// Register a 1×1 solid-color texture under a logical key. With
        /// <paramref name="overwrite"/> the previous texture is replaced (used for
        /// per-dungeon palettes); otherwise an existing key is left untouched.
        /// </summary>
        public void RegisterSolid(string key, Color color, bool overwrite = false)
        {
            if (_textures.TryGetValue(key, out Texture2D existing))
            {
                if (!overwrite) return;
                existing?.Dispose();
            }

            var texture = new Texture2D(_graphicsDevice, 1, 1);
            texture.SetData(new[] { color });
            _textures[key] = texture;
        }

        /// <summary>Load a texture from the content pipeline (Sprites/), or the placeholder on failure.</summary>
        public Texture2D LoadTexture(string assetName)
        {
            try
            {
                return _contentManager.Load<Texture2D>(Path.Combine(SpritePath, assetName));
            }
            catch (ContentLoadException ex)
            {
                Console.WriteLine($"Failed to load texture: {assetName} - {ex.Message}");
                return GetPlaceholderTexture();
            }
        }

        public SpriteFont LoadFont(string fontName)
        {
            if (_fonts.TryGetValue(fontName, out SpriteFont cached)) return cached;

            try
            {
                SpriteFont font = _contentManager.Load<SpriteFont>(Path.Combine(FontPath, fontName));
                _fonts[fontName] = font;
                return font;
            }
            catch (ContentLoadException ex)
            {
                Console.WriteLine($"Failed to load font: {fontName} - {ex.Message}");
                return null;
            }
        }

        /// <summary>Load a raw data file (JSON, XML, ...) from Content/Data.</summary>
        public string LoadDataFile(string fileName)
        {
            try
            {
                string fullPath = Path.Combine(_contentManager.RootDirectory, DataPath, fileName);
                return File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load data file: {fileName} - {ex.Message}");
                return null;
            }
        }

        /// <summary>Shared purple/pink checkerboard shown when an asset is missing.</summary>
        private Texture2D GetPlaceholderTexture()
        {
            if (_placeholder != null) return _placeholder;

            const int size = 32;
            var texture = new Texture2D(_graphicsDevice, size, size);
            var data = new Color[size * size];
            for (int i = 0; i < data.Length; i++)
            {
                int x = i % size;
                int y = i / size;
                data[i] = ((x / 8) + (y / 8)) % 2 == 0 ? Color.Purple : Color.Pink;
            }
            texture.SetData(data);
            _placeholder = texture;
            return texture;
        }

        public void UnloadAll()
        {
            foreach (Texture2D texture in _textures.Values)
            {
                texture?.Dispose();
            }
            _textures.Clear();
            _fonts.Clear();

            _placeholder?.Dispose();
            _placeholder = null;

            _contentManager.Unload();
        }

        public void Dispose() => UnloadAll();
    }
}
