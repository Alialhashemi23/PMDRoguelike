using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;

namespace PMDRoguelike.States
{
    /// <summary>The front door: new game or quit.</summary>
    public class TitleState : GameState
    {
        private float _pulse;

        public TitleState(PMDRogueGame game) : base(game) { }

        public override void Enter() => Game.Audio?.PlayMusic("title");

        public override void Update(GameTime gameTime)
        {
            _pulse += (float)gameTime.ElapsedGameTime.TotalSeconds;

            KeyboardManager keyboard = KeyboardManager.Instance;
            if (keyboard.WasKeyJustPressed(Keys.Enter))
            {
                Core.AudioCues.Post("menu");
                Game.States.ChangeState(new StarterSelectState(Game));
            }
            else if (keyboard.WasKeyJustPressed(Keys.Escape))
            {
                Game.Exit();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Game.GraphicsDevice.Clear(new Color(14, 16, 26));

            var spriteBatch = Game.SpriteBatch;
            spriteBatch.Begin();

            TextRenderer.DrawCentered(spriteBatch, Game, "PROJECT PMD-ROGUE", -120, new Color(255, 210, 100), 2.0f);
            TextRenderer.DrawCentered(spriteBatch, Game, "A Mystery Dungeon roguelike with a Risk of Rain heart",
                -60, new Color(160, 165, 185), 0.9f);

            float blink = 0.55f + 0.45f * System.MathF.Sin(_pulse * 3f);
            TextRenderer.DrawCentered(spriteBatch, Game, "Press Enter to begin", 40, Color.White * blink, 1.1f);
            TextRenderer.DrawCentered(spriteBatch, Game, "Esc to quit", 90, Color.Gray, 0.85f);

            TextRenderer.DrawCentered(spriteBatch, Game, "Permadeath: every run starts from nothing.", 180,
                new Color(120, 120, 135), 0.75f);

            spriteBatch.End();
        }
    }
}
