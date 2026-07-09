using Microsoft.Xna.Framework;
using System;

namespace PMDRoguelike.Rendering
{
    /// <summary>
    /// Smoothly follows a world-space focus point (the player's render position)
    /// and produces the view matrix for SpriteBatch.
    /// </summary>
    public class Camera
    {
        private const float FollowSharpness = 8f;

        /// <summary>World-space point the camera is centered on.</summary>
        public Vector2 Position { get; private set; }

        private bool _initialized;

        public void Update(Vector2 focus, float deltaSeconds)
        {
            if (!_initialized)
            {
                Position = focus;
                _initialized = true;
                return;
            }

            // Framerate-independent exponential smoothing.
            float t = 1f - MathF.Exp(-FollowSharpness * deltaSeconds);
            Position = Vector2.Lerp(Position, focus, t);
        }

        public Matrix GetViewMatrix(int viewportWidth, int viewportHeight) =>
            Matrix.CreateTranslation(
                MathF.Round(viewportWidth / 2f - Position.X),
                MathF.Round(viewportHeight / 2f - Position.Y),
                0f);
    }
}
