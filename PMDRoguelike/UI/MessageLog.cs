using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PMDRoguelike.Rendering;
using System.Collections.Generic;

namespace PMDRoguelike.UI
{
    /// <summary>
    /// Scrolling combat/event log. Adding messages is graphics-free (used headlessly
    /// in tests); drawing shows the most recent lines bottom-left with a backdrop.
    /// </summary>
    public class MessageLog
    {
        private const int Capacity = 100;
        private const int VisibleLines = 5;
        private const float TextScale = 0.75f;

        private readonly List<string> _messages = new();

        public IReadOnlyList<string> Messages => _messages;

        public void Add(string message)
        {
            _messages.Add(message);
            if (_messages.Count > Capacity) _messages.RemoveAt(0);
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel,
            int viewportWidth, int viewportHeight)
        {
            if (font == null || _messages.Count == 0) return;

            float lineHeight = font.LineSpacing * TextScale;
            int lines = System.Math.Min(VisibleLines, _messages.Count);
            float boxHeight = lines * lineHeight + 12;
            float boxTop = viewportHeight - boxHeight - 8;

            spriteBatch.Draw(pixel, new Rectangle(8, (int)boxTop, 560, (int)boxHeight), Color.Black * 0.55f);

            for (int i = 0; i < lines; i++)
            {
                string message = _messages[_messages.Count - lines + i];
                float age = lines - 1 - i;
                Color color = Color.White * (1f - age * 0.15f);
                TextRenderer.DrawShadowed(spriteBatch, font, message,
                    new Vector2(16, boxTop + 6 + i * lineHeight), color, TextScale);
            }
        }
    }
}
