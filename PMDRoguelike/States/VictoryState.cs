using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;

namespace PMDRoguelike.States
{
    /// <summary>
    /// Placeholder victory screen reached after clearing the final dungeon.
    /// Phase 8 replaces it with a proper run-stats screen.
    /// </summary>
    public class VictoryState : GameState
    {
        private readonly int _totalTurns;

        public VictoryState(PMDRogueGame game, int totalTurns) : base(game)
        {
            _totalTurns = totalTurns;
        }

        public override void Update(GameTime gameTime)
        {
            if (KeyboardManager.Instance.WasKeyJustPressed(Keys.Enter))
                Game.States.ChangeState(new DungeonState(Game));
        }

        public override void Draw(GameTime gameTime)
        {
            Game.GraphicsDevice.Clear(new Color(10, 20, 14));

            var spriteBatch = Game.SpriteBatch;
            spriteBatch.Begin();
            TextRenderer.DrawCentered(spriteBatch, Game, "VICTORY!", -60, new Color(120, 220, 120), 1.6f);
            TextRenderer.DrawCentered(spriteBatch, Game, $"All dungeons cleared in {_totalTurns} turns.", 0, Color.LightGray, 1f);
            TextRenderer.DrawCentered(spriteBatch, Game, "Press Enter to begin a new run", 60, Color.Gray, 1f);
            spriteBatch.End();
        }
    }
}
