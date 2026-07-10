using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;

namespace PMDRoguelike.States
{
    /// <summary>Victory screen after clearing the final dungeon, with run tallies.</summary>
    public class VictoryState : GameState
    {
        private readonly int _totalTurns;
        private readonly Player _player;

        public VictoryState(PMDRogueGame game, int totalTurns, Player player = null) : base(game)
        {
            _totalTurns = totalTurns;
            _player = player;
        }

        public override void Enter()
        {
            Game.Audio?.PlayMusic("title");
            Core.AudioCues.Post("levelup");
        }

        public override void Update(GameTime gameTime)
        {
            if (KeyboardManager.Instance.WasKeyJustPressed(Keys.Enter))
                Game.States.ChangeState(new TitleState(Game));
        }

        public override void Draw(GameTime gameTime)
        {
            Game.GraphicsDevice.Clear(new Color(10, 20, 14));

            var spriteBatch = Game.SpriteBatch;
            spriteBatch.Begin();
            TextRenderer.DrawCentered(spriteBatch, Game, "VICTORY!", -140, new Color(120, 220, 120), 1.6f);
            TextRenderer.DrawCentered(spriteBatch, Game, "All three dungeons cleared. The realm is at peace.", -80, Color.LightGray, 1f);
            RunSummary.Draw(spriteBatch, Game, _player, _totalTurns, startYOffset: -30);
            TextRenderer.DrawCentered(spriteBatch, Game, "Press Enter to return to the title", 150, Color.Gray, 1f);
            spriteBatch.End();
        }
    }
}
