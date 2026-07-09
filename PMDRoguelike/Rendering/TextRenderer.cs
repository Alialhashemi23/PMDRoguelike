using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PMDRoguelike.Rendering
{
    /// <summary>
    /// Small text helpers shared by HUD and menu screens.
    /// All methods no-op gracefully if the font failed to load.
    /// </summary>
    public static class TextRenderer
    {
        public static void DrawShadowed(SpriteBatch spriteBatch, SpriteFont font, string text,
            Vector2 position, Color color, float scale = 1f)
        {
            if (font == null || string.IsNullOrEmpty(text)) return;

            spriteBatch.DrawString(font, text, position + new Vector2(1, 1), Color.Black * 0.7f,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, text, position, color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>Draw a line horizontally centered on screen, offset vertically from center.</summary>
        public static void DrawCentered(SpriteBatch spriteBatch, PMDRogueGame game, string text,
            float yOffset, Color color, float scale = 1f)
        {
            SpriteFont font = game.GameContent.LoadFont("Default");
            if (font == null || string.IsNullOrEmpty(text)) return;

            var viewport = game.GraphicsDevice.Viewport;
            Vector2 size = font.MeasureString(text) * scale;
            var position = new Vector2(
                (viewport.Width - size.X) / 2f,
                (viewport.Height - size.Y) / 2f + yOffset);
            DrawShadowed(spriteBatch, font, text, position, color, scale);
        }
    }
}
