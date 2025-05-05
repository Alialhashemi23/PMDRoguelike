using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMDRoguelike.Managers
{
    public class GameContentManager : IDisposable
    {
        private ContentManager _contentManager;
        private GraphicsDevice _graphicsDevice;

        // Asset caches
        private Dictionary<string, Texture2D> _textures;
        private Dictionary<string, SpriteFont> _fonts;

        // Content paths (from constants)
        private readonly string SPRITE_PATH = "Sprites/";
        private readonly string AUDIO_PATH = "Audio/";
        private readonly string FONT_PATH = "Fonts/";
        private readonly string MAP_PATH = "Maps/";
        private readonly string DATA_PATH = "Data/";

        public GameContentManager(ContentManager contentManager, GraphicsDevice graphicsDevice)
        {
            _contentManager = contentManager;
            _graphicsDevice = graphicsDevice;

            // Initialize caches
            _textures = new Dictionary<string, Texture2D>();
            _fonts = new Dictionary<string, SpriteFont>();
        }

        /// <summary>
        /// Load a texture from file or cache
        /// </summary>
        public Texture2D LoadTexture(string assetName)
        {
            // Check cache first
            if (_textures.ContainsKey(assetName))
            {
                return _textures[assetName];
            }

            try
            {
                // Load from content pipeline
                string fullPath = Path.Combine(SPRITE_PATH, assetName);
                Texture2D texture = _contentManager.Load<Texture2D>(fullPath);

                // Cache the texture
                _textures[assetName] = texture;

                return texture;
            }
            catch (ContentLoadException ex)
            {
                Console.WriteLine($"Failed to load texture: {assetName} - {ex.Message}");
                return CreatePlaceholderTexture();
            }
        }

        /// <summary>
        /// Load a sprite font from file or cache
        /// </summary>
        public SpriteFont LoadFont(string fontName)
        {
            // Check cache first
            if (_fonts.ContainsKey(fontName))
            {
                return _fonts[fontName];
            }

            try
            {
                // Load from content pipeline
                string fullPath = Path.Combine(FONT_PATH, fontName);
                SpriteFont font = _contentManager.Load<SpriteFont>(fullPath);

                // Cache the font
                _fonts[fontName] = font;

                return font;
            }
            catch (ContentLoadException ex)
            {
                Console.WriteLine($"Failed to load font: {fontName} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load raw data file (JSON, XML, etc.)
        /// </summary>
        public string LoadDataFile(string fileName)
        {
            try
            {
                string fullPath = Path.Combine(_contentManager.RootDirectory, DATA_PATH, fileName);
                return File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load data file: {fileName} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a placeholder texture when loading fails
        /// </summary>
        private Texture2D CreatePlaceholderTexture()
        {
            Texture2D texture = new Texture2D(_graphicsDevice, 32, 32);
            Color[] data = new Color[32 * 32];

            // Create a checkerboard pattern
            for (int i = 0; i < data.Length; i++)
            {
                int x = i % 32;
                int y = i / 32;
                data[i] = ((x / 8) + (y / 8)) % 2 == 0 ? Color.Purple : Color.Pink;
            }

            texture.SetData(data);
            return texture;
        }

        /// <summary>
        /// Unload all cached assets
        /// </summary>
        public void UnloadAll()
        {
            foreach (var texture in _textures.Values)
            {
                texture?.Dispose();
            }
            _textures.Clear();

            _fonts.Clear();

            _contentManager.Unload();
        }

        public void Dispose()
        {
            UnloadAll();
        }
    }

    /// <summary>
    /// Asset Registry to manage loaded assets by category
    /// </summary>
    public class AssetRegistry
    {
        private GameContentManager _contentManager;

        // Dictionaries for different asset types
        public Dictionary<string, Texture2D> PokemonSprites { get; private set; }
        public Dictionary<string, Texture2D> TileSprites { get; private set; }
        public Dictionary<string, Texture2D> ItemSprites { get; private set; }
        public Dictionary<string, Texture2D> UISprites { get; private set; }
        public Dictionary<string, SpriteFont> Fonts { get; private set; }

        public AssetRegistry(GameContentManager contentManager)
        {
            _contentManager = contentManager;

            // Initialize collections
            PokemonSprites = new Dictionary<string, Texture2D>();
            TileSprites = new Dictionary<string, Texture2D>();
            ItemSprites = new Dictionary<string, Texture2D>();
            UISprites = new Dictionary<string, Texture2D>();
            Fonts = new Dictionary<string, SpriteFont>();
        }

        /// <summary>
        /// Load core assets required for the game
        /// </summary>
        public void LoadCoreAssets()
        {
            // Load default font
            Fonts["default"] = _contentManager.LoadFont("default");

            // Load placeholder sprites
            PokemonSprites["placeholder"] = _contentManager.LoadTexture("pokemon/placeholder");
            TileSprites["wall"] = _contentManager.LoadTexture("tiles/wall");
            TileSprites["floor"] = _contentManager.LoadTexture("tiles/floor");
            ItemSprites["potion"] = _contentManager.LoadTexture("items/potion");

            // Load UI elements
            UISprites["healthbar"] = _contentManager.LoadTexture("ui/healthbar");
            UISprites["menubutton"] = _contentManager.LoadTexture("ui/menubutton");
        }

        /// <summary>
        /// Load a Pokemon sprite by name
        /// </summary>
        public Texture2D GetPokemonSprite(string pokemonName)
        {
            if (!PokemonSprites.ContainsKey(pokemonName))
            {
                // Try to load it
                PokemonSprites[pokemonName] = _contentManager.LoadTexture($"pokemon/{pokemonName}");
            }

            // Return sprite or placeholder if not found
            return PokemonSprites[pokemonName] ?? PokemonSprites["placeholder"];
        }
    }

    /// <summary>
    /// Updated main game class with content pipeline integration
    /// </summary>
    public class PokemonMDGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Content management
        private GameContentManager _gameContent;
        private AssetRegistry _assetRegistry;

        protected override void Initialize()
        {
            // Initialize content manager
            _gameContent = new GameContentManager(Content, GraphicsDevice);
            _assetRegistry = new AssetRegistry(_gameContent);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load core assets
            _assetRegistry.LoadCoreAssets();

            // Load constants
            string constantsJson = _gameContent.LoadDataFile("GameConstants.json");
            // Parse and apply constants...
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            // Example: Draw a Pokemon sprite
            Texture2D bulbasaur = _assetRegistry.GetPokemonSprite("bulbasaur");
            _spriteBatch.Draw(bulbasaur, new Vector2(100, 100), Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _gameContent?.Dispose();
            base.UnloadContent();
        }
    }
}
