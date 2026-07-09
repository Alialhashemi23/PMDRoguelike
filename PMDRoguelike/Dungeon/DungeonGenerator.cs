using Microsoft.Xna.Framework;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using System.Collections.Generic;

namespace PMDRoguelike.Dungeon
{
    /// <summary>
    /// Output of a floor generation pass: the map plus suggested spawn points.
    /// </summary>
    public class GeneratedFloor
    {
        public DungeonMap Map { get; init; }
        public Point PlayerSpawn { get; init; }
        public List<Point> EnemySpawns { get; init; }
    }

    /// <summary>
    /// Classic rooms-and-corridors generator: place non-overlapping rooms,
    /// then connect consecutive rooms with L-shaped corridors (guarantees connectivity).
    /// Parameters come from GameConstants.WorldGeneration.
    /// </summary>
    public class DungeonGenerator
    {
        private readonly Rng _rng;

        public DungeonGenerator(Rng rng)
        {
            _rng = rng;
        }

        /// <summary>
        /// Generate one floor. When a dungeon definition is given, its dimensions
        /// override the global defaults from GameConstants.
        /// </summary>
        public GeneratedFloor Generate(DungeonDefinition definition = null)
        {
            DungeonConstants cfg = GameConstants.Instance.Data.WorldGeneration.Dungeons;
            SpawningConstants spawnCfg = GameConstants.Instance.Data.WorldGeneration.Spawning;

            int width = definition?.FloorWidth > 0 ? definition.FloorWidth : cfg.FloorWidth;
            int height = definition?.FloorHeight > 0 ? definition.FloorHeight : cfg.FloorHeight;
            var map = new DungeonMap(width, height);

            PlaceRooms(map, cfg);
            ConnectRooms(map);

            Point playerSpawn = RandomPointInRoom(map.Rooms[0]);
            Point stairs = PlaceStairs(map, playerSpawn);
            List<Point> enemySpawns = PickEnemySpawns(map, spawnCfg, playerSpawn, stairs);

            return new GeneratedFloor { Map = map, PlayerSpawn = playerSpawn, EnemySpawns = enemySpawns };
        }

        /// <summary>
        /// Put the stairs in the room farthest from the player's starting room
        /// so every floor asks for some traversal.
        /// </summary>
        private Point PlaceStairs(DungeonMap map, Point playerSpawn)
        {
            Rectangle spawnRoom = map.Rooms[0];
            Rectangle stairsRoom = spawnRoom;
            int bestDistanceSq = -1;
            foreach (Rectangle room in map.Rooms)
            {
                int dx = room.Center.X - spawnRoom.Center.X;
                int dy = room.Center.Y - spawnRoom.Center.Y;
                int distanceSq = dx * dx + dy * dy;
                if (distanceSq > bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    stairsRoom = room;
                }
            }

            Point stairs = RandomPointInRoom(stairsRoom);
            for (int attempts = 0; stairs == playerSpawn && attempts < 50; attempts++)
                stairs = RandomPointInRoom(stairsRoom);

            map.SetTile(stairs, TileType.Stairs);
            map.StairsPosition = stairs;
            return stairs;
        }

        private void PlaceRooms(DungeonMap map, DungeonConstants cfg)
        {
            int attempts = cfg.MaxRoomsPerFloor * 6;
            while (map.Rooms.Count < cfg.MaxRoomsPerFloor && attempts-- > 0)
            {
                int w = _rng.Next(cfg.MinRoomSize, cfg.MaxRoomSize + 1);
                int h = _rng.Next(cfg.MinRoomSize, cfg.MaxRoomSize + 1);
                // Keep a 1-tile wall border around the map edge.
                if (w > map.Width - 2 || h > map.Height - 2) continue;
                int x = _rng.Next(1, map.Width - w - 1);
                int y = _rng.Next(1, map.Height - h - 1);

                var room = new Rectangle(x, y, w, h);
                var padded = new Rectangle(x - 1, y - 1, w + 2, h + 2);

                bool overlaps = false;
                foreach (Rectangle existing in map.Rooms)
                {
                    if (padded.Intersects(existing)) { overlaps = true; break; }
                }
                if (overlaps) continue;

                CarveRoom(map, room);
                map.Rooms.Add(room);
            }

            // A floor needs at least somewhere to stand; force one central room if placement failed.
            if (map.Rooms.Count == 0)
            {
                var fallback = new Rectangle(map.Width / 2 - 3, map.Height / 2 - 3, 6, 6);
                CarveRoom(map, fallback);
                map.Rooms.Add(fallback);
            }
        }

        private static void CarveRoom(DungeonMap map, Rectangle room)
        {
            for (int x = room.Left; x < room.Right; x++)
                for (int y = room.Top; y < room.Bottom; y++)
                    map.SetTile(new Point(x, y), TileType.Floor);
        }

        private void ConnectRooms(DungeonMap map)
        {
            for (int i = 1; i < map.Rooms.Count; i++)
            {
                Point a = map.Rooms[i - 1].Center;
                Point b = map.Rooms[i].Center;

                if (_rng.Chance(0.5f))
                {
                    CarveHorizontal(map, a.X, b.X, a.Y);
                    CarveVertical(map, a.Y, b.Y, b.X);
                }
                else
                {
                    CarveVertical(map, a.Y, b.Y, a.X);
                    CarveHorizontal(map, a.X, b.X, b.Y);
                }
            }
        }

        private static void CarveHorizontal(DungeonMap map, int x1, int x2, int y)
        {
            for (int x = System.Math.Min(x1, x2); x <= System.Math.Max(x1, x2); x++)
                map.SetTile(new Point(x, y), TileType.Floor);
        }

        private static void CarveVertical(DungeonMap map, int y1, int y2, int x)
        {
            for (int y = System.Math.Min(y1, y2); y <= System.Math.Max(y1, y2); y++)
                map.SetTile(new Point(x, y), TileType.Floor);
        }

        private Point RandomPointInRoom(Rectangle room) =>
            new Point(_rng.Next(room.Left, room.Right), _rng.Next(room.Top, room.Bottom));

        private List<Point> PickEnemySpawns(DungeonMap map, SpawningConstants cfg, Point playerSpawn, Point stairs)
        {
            var spawns = new List<Point>();
            int count = _rng.Next(cfg.MinEnemiesPerFloor, cfg.MaxEnemiesPerFloor + 1);

            int attempts = count * 20;
            while (spawns.Count < count && attempts-- > 0)
            {
                // Prefer rooms other than the player's starting room when there is more than one.
                Rectangle room = map.Rooms.Count > 1
                    ? map.Rooms[_rng.Next(1, map.Rooms.Count)]
                    : map.Rooms[0];

                Point p = RandomPointInRoom(room);
                if (p == playerSpawn || p == stairs || spawns.Contains(p)) continue;
                spawns.Add(p);
            }

            return spawns;
        }
    }
}
