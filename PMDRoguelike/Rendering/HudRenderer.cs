using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PMDRoguelike.Run;

namespace PMDRoguelike.Rendering
{
    /// <summary>
    /// In-dungeon overlay: dungeon name, floor progress, turn count, and the
    /// descend prompt. Drawn in screen space (no camera transform).
    /// </summary>
    public class HudRenderer
    {
        private readonly SpriteFont _font;

        public HudRenderer(SpriteFont font)
        {
            _font = font;
        }

        public void Draw(SpriteBatch spriteBatch, RunManager run, int floorTurns, bool onStairs,
            int viewportWidth, int viewportHeight)
        {
            if (_font == null) return;

            string location = $"{run.CurrentDungeon.Name}  —  F{run.FloorNumber}/{run.CurrentDungeon.Floors}";
            TextRenderer.DrawShadowed(spriteBatch, _font, location, new Vector2(12, 8), Color.White);

            string turns = $"Turns: {run.TotalTurns + floorTurns}";
            Vector2 turnsSize = _font.MeasureString(turns);
            TextRenderer.DrawShadowed(spriteBatch, _font, turns,
                new Vector2(viewportWidth - turnsSize.X - 12, 8), new Color(200, 200, 200));

            if (onStairs)
            {
                string prompt = "Press Enter to descend";
                Vector2 promptSize = _font.MeasureString(prompt);
                TextRenderer.DrawShadowed(spriteBatch, _font, prompt,
                    new Vector2((viewportWidth - promptSize.X) / 2f, viewportHeight - promptSize.Y - 24),
                    new Color(150, 190, 255));
            }
        }
    }
}
