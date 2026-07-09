using Microsoft.Xna.Framework;
using PMDRoguelike.Core;
using PMDRoguelike.Entities;
using System.Collections.Generic;

namespace PMDRoguelike.Dungeon
{
    /// <summary>
    /// The tile grid for a single dungeon floor, plus the actors standing on it.
    /// All movement legality (bounds, walls, corner-cutting) is answered here.
    /// </summary>
    public class DungeonMap
    {
        private readonly Tile[,] _tiles;

        public int Width { get; }
        public int Height { get; }

        /// <summary>Rooms carved by the generator (useful for spawning and later features).</summary>
        public List<Rectangle> Rooms { get; } = new List<Rectangle>();

        /// <summary>Where the stairs to the next floor are (set by the generator).</summary>
        public Point StairsPosition { get; set; }

        /// <summary>All actors on this floor. Index 0 is conventionally the player.</summary>
        public List<Actor> Actors { get; } = new List<Actor>();

        public DungeonMap(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new Tile[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _tiles[x, y] = new Tile(TileType.Wall);
        }

        public bool InBounds(Point p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

        public Tile GetTile(Point p) => _tiles[p.X, p.Y];

        public void SetTile(Point p, TileType type) => _tiles[p.X, p.Y] = new Tile(type);

        public bool IsWalkable(Point p) => InBounds(p) && _tiles[p.X, p.Y].IsWalkable;

        /// <summary>
        /// Terrain-only movement check. Diagonal steps are blocked if either adjacent
        /// orthogonal tile is a wall (no cutting corners), matching PMD rules.
        /// Actor occupancy is resolved separately by the TurnController.
        /// </summary>
        public bool CanMove(Point from, Direction direction)
        {
            if (direction == Direction.None) return false;

            Point offset = direction.ToOffset();
            Point target = from + offset;
            if (!IsWalkable(target)) return false;

            if (direction.IsDiagonal())
            {
                if (!IsWalkable(new Point(from.X + offset.X, from.Y))) return false;
                if (!IsWalkable(new Point(from.X, from.Y + offset.Y))) return false;
            }

            return true;
        }

        public Actor GetActorAt(Point p)
        {
            foreach (Actor actor in Actors)
            {
                if (actor.GridPosition == p) return actor;
            }
            return null;
        }

        public bool IsOccupied(Point p) => GetActorAt(p) != null;
    }
}
