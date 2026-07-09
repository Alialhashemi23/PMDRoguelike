using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Core;

namespace PMDRoguelike.Managers
{
    /// <summary>
    /// Tracks current and previous keyboard state so edge events
    /// (just pressed / just released) can be detected. Update() must be
    /// called exactly once per frame, before any queries.
    /// </summary>
    public class KeyboardManager
    {
        private static KeyboardManager _instance;
        public static KeyboardManager Instance => _instance ??= new KeyboardManager();

        private KeyboardState _current;
        private KeyboardState _previous;

        private KeyboardManager()
        {
            _current = Keyboard.GetState();
            _previous = _current;
        }

        public void Update()
        {
            _previous = _current;
            _current = Keyboard.GetState();
        }

        public bool IsKeyDown(Keys key) => _current.IsKeyDown(key);

        public bool WasKeyJustPressed(Keys key) => _current.IsKeyDown(key) && _previous.IsKeyUp(key);

        public bool WasKeyJustReleased(Keys key) => _current.IsKeyUp(key) && _previous.IsKeyDown(key);

        /// <summary>
        /// Current held movement direction from arrow keys or WASD,
        /// combining two cardinals into a diagonal.
        /// </summary>
        public Direction GetHeldDirection()
        {
            bool up = _current.IsKeyDown(Keys.Up) || _current.IsKeyDown(Keys.W);
            bool down = _current.IsKeyDown(Keys.Down) || _current.IsKeyDown(Keys.S);
            bool left = _current.IsKeyDown(Keys.Left) || _current.IsKeyDown(Keys.A);
            bool right = _current.IsKeyDown(Keys.Right) || _current.IsKeyDown(Keys.D);
            return DirectionExtensions.FromInput(up, down, left, right);
        }
    }
}
