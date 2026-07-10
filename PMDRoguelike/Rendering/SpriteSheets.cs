using Microsoft.Xna.Framework;
using PMDRoguelike.Core;

namespace PMDRoguelike.Rendering
{
    /// <summary>
    /// Layout rules for species sheets: 2 frames wide x 4 rows (down, left, right, up),
    /// 32px frames. Walk frames alternate while an entity is sliding between tiles.
    /// </summary>
    public static class SpriteSheets
    {
        public const int FrameSize = 32;
        private const double WalkFrameMs = 130;

        public static Rectangle Source(Direction facing, bool moving, double totalMs)
        {
            int row = facing switch
            {
                Direction.North => 3,
                Direction.East or Direction.NorthEast or Direction.SouthEast => 2,
                Direction.West or Direction.NorthWest or Direction.SouthWest => 1,
                _ => 0 // South / None
            };
            int frame = moving ? (int)(totalMs / WalkFrameMs) % 2 : 0;
            return new Rectangle(frame * FrameSize, row * FrameSize, FrameSize, FrameSize);
        }
    }
}
