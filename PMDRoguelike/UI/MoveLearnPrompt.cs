using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Combat;
using PMDRoguelike.Data;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;

namespace PMDRoguelike.UI
{
    /// <summary>
    /// Modal prompt when leveling offers a fifth move: press 1-4 to replace a known
    /// move, or N to skip learning. Blocks turn input while open.
    /// </summary>
    public class MoveLearnPrompt
    {
        private readonly Player _player;
        private readonly MessageLog _log;
        private readonly MoveDefinition _newMove;

        public MoveLearnPrompt(Player player, MessageLog log)
        {
            _player = player;
            _log = log;
            _newMove = player.PendingMoveLearns[0];
        }

        /// <summary>Returns true once the prompt has been resolved.</summary>
        public bool Update(KeyboardManager keyboard)
        {
            if (keyboard.WasKeyJustPressed(Keys.N))
            {
                _log.Add($"{_player.DisplayName} did not learn {_newMove.Name}.");
                _player.PendingMoveLearns.RemoveAt(0);
                return true;
            }

            for (int i = 0; i < _player.Moves.Count; i++)
            {
                if (keyboard.WasKeyJustPressed(Keys.D1 + i) || keyboard.WasKeyJustPressed(Keys.NumPad1 + i))
                {
                    string forgotten = _player.Moves[i].Move.Name;
                    _player.Moves[i] = new MoveSlot(_newMove);
                    _log.Add($"{_player.DisplayName} forgot {forgotten} and learned {_newMove.Name}!");
                    _player.PendingMoveLearns.RemoveAt(0);
                    return true;
                }
            }

            return false;
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel,
            int viewportWidth, int viewportHeight)
        {
            if (font == null) return;

            const float scale = 0.85f;
            float lineHeight = font.LineSpacing * scale;
            int lines = _player.Moves.Count + 3;
            var box = new Rectangle(viewportWidth / 2 - 240, viewportHeight / 2 - (int)(lines * lineHeight / 2) - 10,
                480, (int)(lines * lineHeight + 20));
            spriteBatch.Draw(pixel, box, Color.Black * 0.85f);

            float y = box.Y + 10;
            TextRenderer.DrawShadowed(spriteBatch, font, $"Wants to learn {_newMove.Name} ({_newMove.Type}, {_newMove.Power} power)!",
                new Vector2(box.X + 14, y), new Color(255, 220, 120), scale);
            y += lineHeight;
            TextRenderer.DrawShadowed(spriteBatch, font, "Press 1-4 to forget a move, or N to skip:",
                new Vector2(box.X + 14, y), Color.LightGray, scale);
            y += lineHeight;

            for (int i = 0; i < _player.Moves.Count; i++)
            {
                MoveSlot slot = _player.Moves[i];
                TextRenderer.DrawShadowed(spriteBatch, font,
                    $"[{i + 1}] {slot.Move.Name}  {slot.CurrentPP}/{slot.Move.PP}  {slot.Move.Type}",
                    new Vector2(box.X + 14, y), Color.White, scale);
                y += lineHeight;
            }
        }
    }
}
