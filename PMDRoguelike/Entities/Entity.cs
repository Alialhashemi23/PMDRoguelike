using Microsoft.Xna.Framework;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;

namespace PMDRoguelike.Entities
{
    /// <summary>
    /// Anything that exists on the grid. Owns the split between logical position
    /// (GridPosition, always tile-exact) and visual position (RenderPosition, which
    /// slides smoothly between tiles). Game logic must only ever read GridPosition.
    /// </summary>
    public abstract class Entity
    {
        private Vector2 _slideFrom;
        private Vector2 _slideTo;
        private float _slideElapsedMs;
        private float _slideDurationMs;
        private bool _sliding;

        private const float LungeDurationMs = 140f;
        private float _lungeElapsedMs;
        private bool _lunging;
        private Direction _lungeDirection;

        private const float FlashDurationMs = 160f;
        private float _flashRemainingMs;

        public Point GridPosition { get; private set; }

        /// <summary>Top-left pixel position used for drawing (excluding attack offsets).</summary>
        public Vector2 RenderPosition { get; private set; }

        /// <summary>Extra draw offset from the attack lunge animation.</summary>
        public Vector2 VisualOffset { get; private set; }

        /// <summary>0..1 intensity of the "just got hit" flash for the renderer.</summary>
        public float HitFlash => _flashRemainingMs <= 0f ? 0f : _flashRemainingMs / FlashDurationMs;

        public bool IsAnimating => _sliding || _lunging;

        /// <summary>Logical texture key resolved by the renderer.</summary>
        public string SpriteKey { get; protected set; } = "entity.unknown";

        public Direction Facing { get; set; } = Direction.South;

        protected Entity(Point gridPosition)
        {
            SnapTo(gridPosition);
        }

        /// <summary>Place the entity on a tile with no animation.</summary>
        public void SnapTo(Point gridPosition)
        {
            GridPosition = gridPosition;
            RenderPosition = ToPixels(gridPosition);
            VisualOffset = Vector2.Zero;
            _sliding = false;
            _lunging = false;
        }

        /// <summary>
        /// Commit a move: the logical position updates immediately, while the
        /// render position slides to the new tile over the configured duration.
        /// </summary>
        public void BeginMove(Point target)
        {
            GridPosition = target;
            _slideFrom = RenderPosition;
            _slideTo = ToPixels(target);
            _slideElapsedMs = 0f;
            _slideDurationMs = GameConstants.Instance.SlideDurationMs;
            _sliding = _slideDurationMs > 0f;
            if (!_sliding) RenderPosition = _slideTo;
        }

        /// <summary>Quick out-and-back hop toward the current facing (attack feedback).</summary>
        public void BeginLunge()
        {
            _lungeDirection = Facing;
            _lungeElapsedMs = 0f;
            _lunging = true;
        }

        /// <summary>Start the white "took damage" flash.</summary>
        public void FlashHit() => _flashRemainingMs = FlashDurationMs;

        public void UpdateAnimation(float deltaMs)
        {
            if (_flashRemainingMs > 0f) _flashRemainingMs -= deltaMs;

            if (_sliding)
            {
                _slideElapsedMs += deltaMs;
                if (_slideElapsedMs >= _slideDurationMs)
                {
                    RenderPosition = _slideTo;
                    _sliding = false;
                }
                else
                {
                    RenderPosition = Vector2.Lerp(_slideFrom, _slideTo, _slideElapsedMs / _slideDurationMs);
                }
            }

            if (_lunging)
            {
                _lungeElapsedMs += deltaMs;
                if (_lungeElapsedMs >= LungeDurationMs)
                {
                    VisualOffset = Vector2.Zero;
                    _lunging = false;
                }
                else
                {
                    // Triangle wave: out for the first half, back for the second.
                    float t = _lungeElapsedMs / LungeDurationMs;
                    float amplitude = (t < 0.5f ? t * 2f : (1f - t) * 2f) * GameConstants.Instance.TileSize * 0.35f;
                    Point offset = _lungeDirection.ToOffset();
                    VisualOffset = new Vector2(offset.X, offset.Y) * amplitude;
                }
            }
        }

        private static Vector2 ToPixels(Point gridPosition)
        {
            int tileSize = GameConstants.Instance.TileSize;
            return new Vector2(gridPosition.X * tileSize, gridPosition.Y * tileSize);
        }
    }
}
