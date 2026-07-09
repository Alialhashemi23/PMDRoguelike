using Microsoft.Xna.Framework;
using PMDRoguelike.Constants;

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

        public Point GridPosition { get; private set; }

        /// <summary>Top-left pixel position used for drawing.</summary>
        public Vector2 RenderPosition { get; private set; }

        public bool IsAnimating { get; private set; }

        /// <summary>Logical texture key resolved by the renderer.</summary>
        public string SpriteKey { get; protected set; } = "entity.unknown";

        protected Entity(Point gridPosition)
        {
            SnapTo(gridPosition);
        }

        /// <summary>Place the entity on a tile with no animation.</summary>
        public void SnapTo(Point gridPosition)
        {
            GridPosition = gridPosition;
            RenderPosition = ToPixels(gridPosition);
            IsAnimating = false;
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
            IsAnimating = _slideDurationMs > 0f;
            if (!IsAnimating) RenderPosition = _slideTo;
        }

        public void UpdateAnimation(float deltaMs)
        {
            if (!IsAnimating) return;

            _slideElapsedMs += deltaMs;
            if (_slideElapsedMs >= _slideDurationMs)
            {
                RenderPosition = _slideTo;
                IsAnimating = false;
                return;
            }

            float t = _slideElapsedMs / _slideDurationMs;
            RenderPosition = Vector2.Lerp(_slideFrom, _slideTo, t);
        }

        private static Vector2 ToPixels(Point gridPosition)
        {
            int tileSize = GameConstants.Instance.TileSize;
            return new Vector2(gridPosition.X * tileSize, gridPosition.Y * tileSize);
        }
    }
}
