using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PMDRoguelike.Combat;
using PMDRoguelike.Entities;
using PMDRoguelike.Rendering;

namespace PMDRoguelike.UI
{
    /// <summary>
    /// Move list overlay shown while Shift is held: slot number, name, PP, type, power.
    /// </summary>
    public static class MovePanel
    {
        private const float Scale = 0.75f;

        public static void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel,
            Player player, int viewportWidth)
        {
            if (font == null) return;

            float lineHeight = font.LineSpacing * Scale;
            int lines = player.Moves.Count + 1;
            var box = new Rectangle(viewportWidth - 372, 44, 360, (int)(lines * lineHeight + 14));
            spriteBatch.Draw(pixel, box, Color.Black * 0.65f);

            TextRenderer.DrawShadowed(spriteBatch, font, "Moves  (press 1-4 to use)",
                new Vector2(box.X + 10, box.Y + 6), Color.LightGray, Scale);

            for (int i = 0; i < player.Moves.Count; i++)
            {
                MoveSlot slot = player.Moves[i];
                string line = $"[{i + 1}] {slot.Move.Name}  {slot.CurrentPP}/{slot.Move.PP}  {slot.Move.Type}  {slot.Move.Power}";
                Color color = slot.HasPP ? Color.White : Color.DimGray;
                TextRenderer.DrawShadowed(spriteBatch, font, line,
                    new Vector2(box.X + 10, box.Y + 6 + (i + 1) * lineHeight), color, Scale);
            }

            if (player.AllMovesOutOfPP)
            {
                TextRenderer.DrawShadowed(spriteBatch, font, "Out of PP — attacks use Struggle!",
                    new Vector2(box.X + 10, box.Bottom + 4), new Color(230, 150, 120), Scale);
            }
        }
    }
}
