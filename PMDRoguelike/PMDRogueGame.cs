using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;
using PMDRoguelike.Turns;

namespace PMDRoguelike
{
    /// <summary>
    /// Thin MonoGame shell: wires the systems together and delegates per-frame
    /// work to the TurnController (logic) and DungeonRenderer (drawing).
    /// </summary>
    public class PMDRogueGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private GameContentManager _gameContent;
        private DungeonRenderer _renderer;
        private readonly Camera _camera = new();

        private DungeonMap _map;
        private Player _player;
        private TurnController _turnController;
        private Rng _rng;

        public PMDRogueGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            GameConstants.Instance.LoadConstants();

            _graphics.PreferredBackBufferWidth = GameConstants.Instance.WindowWidth;
            _graphics.PreferredBackBufferHeight = GameConstants.Instance.WindowHeight;
            _graphics.ApplyChanges();
            Window.Title = "Project PMD-Rogue";

            _rng = new Rng();
            BuildFloor();

            base.Initialize();
        }

        /// <summary>Generate a fresh floor and (re)spawn all actors on it.</summary>
        private void BuildFloor()
        {
            GeneratedFloor floor = new DungeonGenerator(_rng).Generate();
            _map = floor.Map;

            _player = new Player(floor.PlayerSpawn);
            _map.Actors.Add(_player);
            foreach (Point spawn in floor.EnemySpawns)
            {
                _map.Actors.Add(new Enemy(spawn));
            }

            _turnController = new TurnController(_map, _player, _rng);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _gameContent = new GameContentManager(Content, GraphicsDevice);
            _renderer = new DungeonRenderer(_gameContent);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardManager keyboard = KeyboardManager.Instance;
            keyboard.Update();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
                Exit();

            // Debug helper while there are no stairs yet: R regenerates the floor.
            if (keyboard.WasKeyJustPressed(Keys.R))
                BuildFloor();

            _turnController.Update(gameTime);

            float halfTile = GameConstants.Instance.TileSize / 2f;
            Vector2 focus = _player.RenderPosition + new Vector2(halfTile, halfTile);
            _camera.Update(focus, (float)gameTime.ElapsedGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(24, 24, 32));

            Matrix view = _camera.GetViewMatrix(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: view);
            _renderer.Draw(_spriteBatch, _map);
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
