using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Managers;
using PMDRoguelike.Turns;

namespace PMDRoguelike.Entities
{
    /// <summary>
    /// The player-controlled Pokémon. Translates keyboard state into a TurnAction.
    /// </summary>
    public class Player : Actor
    {
        private float _heldMs;

        public Player(Point gridPosition) : base(gridPosition)
        {
            SpriteKey = "entity.player";
        }

        /// <summary>
        /// Poll input for this frame and return an action once one is committed,
        /// or null if the player hasn't decided yet. A short delay after the first
        /// keypress lets a second key register so diagonals come out clean.
        /// </summary>
        public TurnAction ReadInput(KeyboardManager keyboard, float deltaMs)
        {
            if (keyboard.WasKeyJustPressed(Keys.Space))
            {
                _heldMs = 0f;
                return new WaitAction();
            }

            Direction held = keyboard.GetHeldDirection();
            if (held == Direction.None)
            {
                _heldMs = 0f;
                return null;
            }

            _heldMs += deltaMs;
            if (_heldMs < GameConstants.Instance.InputDelayMs) return null;

            // Not resetting _heldMs means a held key keeps issuing moves turn after turn.
            return new MoveAction(held);
        }
    }
}
