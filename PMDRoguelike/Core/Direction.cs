using Microsoft.Xna.Framework;

namespace PMDRoguelike.Core
{
    /// <summary>
    /// The eight movement directions on the grid, plus None for "no input / no movement".
    /// </summary>
    public enum Direction
    {
        None,
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest
    }

    public static class DirectionExtensions
    {
        /// <summary>
        /// Grid offset for a direction. North is -Y (up on screen).
        /// </summary>
        public static Point ToOffset(this Direction direction) => direction switch
        {
            Direction.North => new Point(0, -1),
            Direction.NorthEast => new Point(1, -1),
            Direction.East => new Point(1, 0),
            Direction.SouthEast => new Point(1, 1),
            Direction.South => new Point(0, 1),
            Direction.SouthWest => new Point(-1, 1),
            Direction.West => new Point(-1, 0),
            Direction.NorthWest => new Point(-1, -1),
            _ => Point.Zero
        };

        public static bool IsDiagonal(this Direction direction) => direction switch
        {
            Direction.NorthEast or Direction.SouthEast or Direction.SouthWest or Direction.NorthWest => true,
            _ => false
        };

        /// <summary>
        /// Combine held cardinal inputs into a single (possibly diagonal) direction.
        /// Opposite inputs cancel out.
        /// </summary>
        public static Direction FromInput(bool up, bool down, bool left, bool right)
        {
            int dx = (right ? 1 : 0) - (left ? 1 : 0);
            int dy = (down ? 1 : 0) - (up ? 1 : 0);
            return FromOffset(dx, dy);
        }

        public static Direction FromOffset(int dx, int dy) => (dx, dy) switch
        {
            (0, -1) => Direction.North,
            (1, -1) => Direction.NorthEast,
            (1, 0) => Direction.East,
            (1, 1) => Direction.SouthEast,
            (0, 1) => Direction.South,
            (-1, 1) => Direction.SouthWest,
            (-1, 0) => Direction.West,
            (-1, -1) => Direction.NorthWest,
            _ => Direction.None
        };
    }
}
