using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;

namespace PMDRoguelike.States
{
    /// <summary>
    /// Placeholder permadeath screen. Phase 3 sends the player here on fainting;
    /// Phase 8 replaces it with a proper run-stats screen.
    /// </summary>
    public class GameOverState : GameState
    {
        private readonly string _message;

        public GameOverState(PMDRogueGame game, string message = "Your journey ends here...") : base(game)
        {
            _message = message;
        }

        public override void Update(GameTime gameTime)
        {
            if (KeyboardManager.Instance.WasKeyJustPressed(Keys.Enter))
                Game.States.ChangeState(new DungeonState(Game));
        }

        public override void Draw(GameTime gameTime)
        {
            Game.GraphicsDevice.Clear(new Color(20, 8, 8));

            var spriteBatch = Game.SpriteBatch;
            spriteBatch.Begin();
            TextRenderer.DrawCentered(spriteBatch, Game, "GAME OVER", -60, new Color(220, 80, 80), 1.6f);
            TextRenderer.DrawCentered(spriteBatch, Game, _message, 0, Color.LightGray, 1f);
            TextRenderer.DrawCentered(spriteBatch, Game, "Press Enter to begin a new run", 60, Color.Gray, 1f);
            spriteBatch.End();
        }
    }
}
