using Microsoft.Xna.Framework;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.Turns;
using System;
using System.Collections.Generic;
using System.Text;

namespace PMDRoguelike.Debugging
{
    /// <summary>
    /// Headless verification of procgen and turn logic (no window needed).
    /// Prints the generated floor as ASCII, flood-fills to prove every floor tile
    /// is reachable, then simulates turns through the real TurnController and
    /// checks movement invariants. Exit code 0 = pass.
    /// </summary>
    public static class SmokeTest
    {
        public static int Run(int seed)
        {
            GameConstants.Instance.LoadConstants();

            var rng = new Rng(seed);
            GeneratedFloor floor = new DungeonGenerator(rng).Generate();
            DungeonMap map = floor.Map;

            var player = new Player(floor.PlayerSpawn);
            map.Actors.Add(player);
            foreach (Point spawn in floor.EnemySpawns)
            {
                map.Actors.Add(new Enemy(spawn));
            }

            Console.WriteLine($"Seed {seed}: {map.Width}x{map.Height} floor, {map.Rooms.Count} rooms, {floor.EnemySpawns.Count} enemies");
            PrintMap(map);

            bool ok = CheckConnectivity(map, floor.PlayerSpawn);
            ok &= SimulateTurns(map, player, rng, turns: 40);

            Console.WriteLine(ok ? "SMOKE TEST PASSED" : "SMOKE TEST FAILED");
            return ok ? 0 : 1;
        }

        private static void PrintMap(DungeonMap map)
        {
            var sb = new StringBuilder();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var p = new Point(x, y);
                    Actor actor = map.GetActorAt(p);
                    sb.Append(actor switch
                    {
                        Player => '@',
                        Enemy => 'e',
                        _ => map.GetTile(p).IsWalkable ? '.' : '#'
                    });
                }
                sb.AppendLine();
            }
            Console.Write(sb.ToString());
        }

        /// <summary>Every floor tile must be reachable from the player spawn (4-directional BFS).</summary>
        private static bool CheckConnectivity(DungeonMap map, Point start)
        {
            int totalFloor = 0;
            for (int x = 0; x < map.Width; x++)
                for (int y = 0; y < map.Height; y++)
                    if (map.GetTile(new Point(x, y)).IsWalkable) totalFloor++;

            var visited = new HashSet<Point> { start };
            var queue = new Queue<Point>();
            queue.Enqueue(start);
            Point[] cardinals = { new(0, -1), new(0, 1), new(-1, 0), new(1, 0) };

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                foreach (Point offset in cardinals)
                {
                    Point next = current + offset;
                    if (map.IsWalkable(next) && visited.Add(next)) queue.Enqueue(next);
                }
            }

            bool ok = visited.Count == totalFloor;
            Console.WriteLine($"Connectivity: {visited.Count}/{totalFloor} floor tiles reachable from spawn — {(ok ? "OK" : "FAIL")}");
            return ok;
        }

        /// <summary>
        /// Drive the real TurnController with random (legal) player moves and check
        /// that no actor ever ends up inside a wall or sharing a tile.
        /// </summary>
        private static bool SimulateTurns(DungeonMap map, Player player, Rng rng, int turns)
        {
            var controller = new TurnController(map, player, rng);
            Direction[] allDirections =
            {
                Direction.North, Direction.NorthEast, Direction.East, Direction.SouthEast,
                Direction.South, Direction.SouthWest, Direction.West, Direction.NorthWest
            };

            int executed = 0;
            for (int i = 0; i < turns; i++)
            {
                var options = new List<Direction>();
                foreach (Direction dir in allDirections)
                {
                    Point target = player.GridPosition + dir.ToOffset();
                    if (map.CanMove(player.GridPosition, dir) && !map.IsOccupied(target)) options.Add(dir);
                }

                TurnAction action = options.Count > 0 ? new MoveAction(rng.Pick(options)) : new WaitAction();
                if (controller.ExecuteTurn(action)) executed++;

                string violation = FindInvariantViolation(map);
                if (violation != null)
                {
                    Console.WriteLine($"Turn simulation: FAIL on turn {i + 1} — {violation}");
                    return false;
                }
            }

            Console.WriteLine($"Turn simulation: {executed}/{turns} turns executed, invariants held — OK");
            return true;
        }

        private static string FindInvariantViolation(DungeonMap map)
        {
            var seen = new HashSet<Point>();
            foreach (Actor actor in map.Actors)
            {
                if (!map.IsWalkable(actor.GridPosition))
                    return $"{actor.GetType().Name} is standing in a wall at {actor.GridPosition}";
                if (!seen.Add(actor.GridPosition))
                    return $"two actors share tile {actor.GridPosition}";
            }
            return null;
        }
    }
}
