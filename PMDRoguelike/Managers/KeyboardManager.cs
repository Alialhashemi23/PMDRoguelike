using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMDRoguelike.Managers
{
    public class KeyboardManager
    {
        private static KeyboardManager _instance;
        private KeyboardManager() { }
        public static KeyboardManager Instance => _instance ??= new KeyboardManager();
        public void Initialize()
        {
            // Initialize keyboard state or any other setup if needed
        }
        public void Update()
        {
            // Update keyboard state or handle input
            // This is where you would check for key presses, etc.
        }
        public bool IsKeyPressed(Keys key)
        {
            return Keyboard.GetState().IsKeyDown(key);
        }
        public bool IsKeyReleased(Keys key)
        {
            return Keyboard.GetState().IsKeyUp(key);
        }
    }
}
