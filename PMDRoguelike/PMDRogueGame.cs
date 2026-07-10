using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Managers;
using PMDRoguelike.States;

namespace PMDRoguelike
{
    /// <summary>
    /// Thin MonoGame shell: owns the shared services (content, sprite batch, RNG)
    /// and delegates per-frame work to the active GameState.
    /// </summary>
    public class PMDRogueGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public SpriteBatch SpriteBatch { get; private set; }
        public GameContentManager GameContent { get; private set; }
        public AudioManager Audio { get; private set; }
        public GameStateManager States { get; } = new();
        public Rng Rng { get; private set; }

        public PMDRogueGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            GameConstants.Instance.LoadConstants();
            Data.GameData.Load();

            _graphics.PreferredBackBufferWidth = GameConstants.Instance.WindowWidth;
            _graphics.PreferredBackBufferHeight = GameConstants.Instance.WindowHeight;
            _graphics.ApplyChanges();
            Window.Title = "Project PMD-Rogue";

            Rng = new Rng();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            GameContent = new GameContentManager(Content, GraphicsDevice);
            GameContent.RegisterSolid("ui.pixel", Color.White);

            Audio = new AudioManager(Content);
            Audio.LoadContent();

            States.ChangeState(new TitleState(this));
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardManager.Instance.Update();

            // Escape is handled per-state (pause in a run, quit from the title).
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            States.Current?.Update(gameTime);
            Audio?.Update();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(24, 24, 32));

            States.Current?.Draw(gameTime);

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            GameContent?.Dispose();
            base.UnloadContent();
        }
    }
}
