using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;

namespace PMDRoguelike.States
{
    /// <summary>
    /// Permadeath screen with the run's tallies. Nothing carries over — Enter
    /// starts a completely fresh run.
    /// </summary>
    public class GameOverState : GameState
    {
        private readonly string _message;
        private readonly Player _player;
        private readonly int _totalTurns;

        public GameOverState(PMDRogueGame game, string message = "Your journey ends here...",
            Player player = null, int totalTurns = 0) : base(game)
        {
            _message = message;
            _player = player;
            _totalTurns = totalTurns;
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
            TextRenderer.DrawCentered(spriteBatch, Game, "GAME OVER", -140, new Color(220, 80, 80), 1.6f);
            TextRenderer.DrawCentered(spriteBatch, Game, _message, -80, Color.LightGray, 1f);
            RunSummary.Draw(spriteBatch, Game, _player, _totalTurns, startYOffset: -30);
            TextRenderer.DrawCentered(spriteBatch, Game, "Press Enter to begin a new run", 150, Color.Gray, 1f);
            spriteBatch.End();
        }
    }
}
