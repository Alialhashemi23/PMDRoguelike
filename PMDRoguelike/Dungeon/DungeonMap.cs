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
        private readonly bool[,] _explored;
        private readonly bool[,] _visible;

        public int Width { get; }
        public int Height { get; }

        /// <summary>Rooms carved by the generator (useful for spawning and later features).</summary>
        public List<Rectangle> Rooms { get; } = new List<Rectangle>();

        /// <summary>Where the stairs to the next floor are (set by the generator).</summary>
        public Point StairsPosition { get; set; }

        /// <summary>False on boss floors until the boss falls; the stairs tile doesn't exist yet.</summary>
        public bool StairsRevealed { get; set; } = true;

        /// <summary>Materialize the stairs tile (boss defeated).</summary>
        public void RevealStairs()
        {
            SetTile(StairsPosition, TileType.Stairs);
            StairsRevealed = true;
        }

        /// <summary>All actors on this floor. Index 0 is conventionally the player.</summary>
        public List<Actor> Actors { get; } = new List<Actor>();

        /// <summary>Items lying on the floor, picked up by walking over them.</summary>
        public List<Items.GroundItem> GroundItems { get; } = new List<Items.GroundItem>();

        /// <summary>Locked chests (pay Poké to open).</summary>
        public List<Items.Chest> Chests { get; } = new List<Items.Chest>();

        /// <summary>Items on display in a shop room (pay to obtain).</summary>
        public List<Items.ShopItem> ShopItems { get; } = new List<Items.ShopItem>();

        /// <summary>Loose Poké piles.</summary>
        public List<Items.MoneyPile> MoneyPiles { get; } = new List<Items.MoneyPile>();

        public Items.GroundItem GroundItemAt(Point p)
        {
            foreach (Items.GroundItem item in GroundItems)
            {
                if (item.Position == p) return item;
            }
            return null;
        }

        public Items.Chest ChestAt(Point p)
        {
            foreach (Items.Chest chest in Chests)
            {
                if (chest.Position == p) return chest;
            }
            return null;
        }

        public Items.ShopItem ShopItemAt(Point p)
        {
            foreach (Items.ShopItem item in ShopItems)
            {
                if (item.Position == p) return item;
            }
            return null;
        }

        public Items.MoneyPile MoneyPileAt(Point p)
        {
            foreach (Items.MoneyPile pile in MoneyPiles)
            {
                if (pile.Position == p) return pile;
            }
            return null;
        }

        /// <summary>Is this tile free of every special feature (for placement rolls)?</summary>
        public bool IsFeatureFree(Point p) =>
            GroundItemAt(p) == null && ChestAt(p) == null && ShopItemAt(p) == null &&
            MoneyPileAt(p) == null && p != StairsPosition;

        public DungeonMap(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new Tile[width, height];
            _explored = new bool[width, height];
            _visible = new bool[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _tiles[x, y] = new Tile(TileType.Wall);
        }

        // --- Fog of war -------------------------------------------------------

        /// <summary>Ever seen (drawn dimmed when out of sight).</summary>
        public bool IsExplored(Point p) => InBounds(p) && _explored[p.X, p.Y];

        /// <summary>Currently in view (actors are only drawn here).</summary>
        public bool IsVisible(Point p) => InBounds(p) && _visible[p.X, p.Y];

        /// <summary>
        /// Recompute what the viewer sees, PMD-style: the whole room they stand in
        /// (plus its walls), or a small radius while in a corridor. Seen tiles stay
        /// explored forever.
        /// </summary>
        public void UpdateVisibility(Point viewer)
        {
            System.Array.Clear(_visible, 0, _visible.Length);

            MarkSeen(new Rectangle(viewer.X - 2, viewer.Y - 2, 5, 5));

            Rectangle? room = RoomContaining(viewer);
            if (room.HasValue)
            {
                Rectangle r = room.Value;
                MarkSeen(new Rectangle(r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2));
            }
        }

        private void MarkSeen(Rectangle area)
        {
            for (int x = System.Math.Max(0, area.Left); x < System.Math.Min(Width, area.Right); x++)
            {
                for (int y = System.Math.Max(0, area.Top); y < System.Math.Min(Height, area.Bottom); y++)
                {
                    _visible[x, y] = true;
                    _explored[x, y] = true;
                }
            }
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

        /// <summary>The room rectangle containing this point, or null (corridors).</summary>
        public Rectangle? RoomContaining(Point p)
        {
            foreach (Rectangle room in Rooms)
            {
                if (room.Contains(p)) return room;
            }
            return null;
        }

        /// <summary>
        /// True when a straight line between the two tiles crosses only walkable
        /// tiles (Bresenham). Used for enemy sight checks.
        /// </summary>
        public bool HasLineOfSight(Point a, Point b)
        {
            int dx = System.Math.Abs(b.X - a.X), dy = -System.Math.Abs(b.Y - a.Y);
            int sx = a.X < b.X ? 1 : -1, sy = a.Y < b.Y ? 1 : -1;
            int err = dx + dy;
            Point current = a;

            while (current != b)
            {
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; current.X += sx; }
                if (e2 <= dx) { err += dx; current.Y += sy; }
                if (current != b && !IsWalkable(current)) return false;
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
