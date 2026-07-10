using Microsoft.Xna.Framework;
using System;

namespace PMDRoguelike.Rendering
{
    /// <summary>
    /// Smoothly follows a world-space focus point (the player's render position)
    /// and produces the view matrix for SpriteBatch. Supports decaying shake and
    /// a zoom that eases toward 1 (boss-floor intro).
    /// </summary>
    public class Camera
    {
        private const float FollowSharpness = 8f;
        private const float ShakeDecayPerSecond = 14f;
        private const float ZoomSharpness = 2.2f;

        private readonly Random _jitter = new();
        private float _shake;
        private float _zoom = 1f;
        private bool _initialized;

        /// <summary>World-space point the camera is centered on.</summary>
        public Vector2 Position { get; private set; }

        public void Update(Vector2 focus, float deltaSeconds)
        {
            if (!_initialized)
            {
                Position = focus;
                _initialized = true;
            }
            else
            {
                // Framerate-independent exponential smoothing.
                float t = 1f - MathF.Exp(-FollowSharpness * deltaSeconds);
                Position = Vector2.Lerp(Position, focus, t);
            }

            _shake = MathF.Max(0f, _shake - ShakeDecayPerSecond * deltaSeconds * MathF.Max(1f, _shake * 0.6f));
            _zoom += (1f - _zoom) * (1f - MathF.Exp(-ZoomSharpness * deltaSeconds));
        }

        /// <summary>Kick the camera (pixels of jitter, decays quickly).</summary>
        public void AddShake(float intensity) => _shake = MathF.Max(_shake, intensity);

        /// <summary>Start zoomed in; Update eases back to 1 (boss intro).</summary>
        public void SetZoom(float zoom) => _zoom = zoom;

        public Matrix GetViewMatrix(int viewportWidth, int viewportHeight)
        {
            float ox = 0f, oy = 0f;
            if (_shake > 0.1f)
            {
                ox = ((float)_jitter.NextDouble() * 2f - 1f) * _shake;
                oy = ((float)_jitter.NextDouble() * 2f - 1f) * _shake;
            }

            return Matrix.CreateTranslation(-MathF.Round(Position.X + ox), -MathF.Round(Position.Y + oy), 0f)
                 * Matrix.CreateScale(_zoom, _zoom, 1f)
                 * Matrix.CreateTranslation(viewportWidth / 2f, viewportHeight / 2f, 0f);
        }
    }
}
