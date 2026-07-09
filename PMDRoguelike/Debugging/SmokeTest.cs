using Microsoft.Xna.Framework;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.Run;
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
            ok &= SimulateFullRun(seed);

            Console.WriteLine(ok ? "SMOKE TEST PASSED" : "SMOKE TEST FAILED");
            return ok ? 0 : 1;
        }

        /// <summary>
        /// Walk an entire run headlessly: every floor of every defined dungeon,
        /// pathing the player to the stairs through the real TurnController and
        /// advancing through the RunManager until victory. Enemies are omitted so
        /// pathing is deterministic (they can't be fought until the combat phase).
        /// </summary>
        private static bool SimulateFullRun(int seed)
        {
            var rng = new Rng(seed + 1);
            var dungeons = DungeonRegistry.Load();
            var run = new RunManager(dungeons);

            int expectedFloors = 0;
            foreach (var d in dungeons) expectedFloors += d.Floors;

            int floorsCleared = 0;
            while (true)
            {
                GeneratedFloor floor = new DungeonGenerator(rng).Generate(run.CurrentDungeon);
                DungeonMap map = floor.Map;
                string where = $"{run.CurrentDungeon.Name} F{run.FloorNumber}";

                if (!map.IsWalkable(map.StairsPosition) || map.GetTile(map.StairsPosition).Type != TileType.Stairs)
                {
                    Console.WriteLine($"Full run: FAIL — no stairs on {where}");
                    return false;
                }

                var player = new Player(floor.PlayerSpawn);
                map.Actors.Add(player);
                var controller = new TurnController(map, player, rng);

                List<Direction> path = FindPath(map, floor.PlayerSpawn, map.StairsPosition);
                if (path == null)
                {
                    Console.WriteLine($"Full run: FAIL — stairs unreachable on {where}");
                    return false;
                }

                foreach (Direction step in path)
                {
                    if (!controller.ExecuteTurn(new MoveAction(step)))
                    {
                        Console.WriteLine($"Full run: FAIL — pathing move rejected on {where}");
                        return false;
                    }
                }

                if (player.GridPosition != map.StairsPosition)
                {
                    Console.WriteLine($"Full run: FAIL — path did not end on stairs on {where}");
                    return false;
                }

                run.AddTurns(controller.TurnCount);
                floorsCleared++;

                if (run.Advance() == AdvanceResult.Victory) break;
            }

            bool ok = floorsCleared == expectedFloors;
            Console.WriteLine($"Full run: {floorsCleared}/{expectedFloors} floors cleared across {dungeons.Count} dungeons in {run.TotalTurns} turns — {(ok ? "OK" : "FAIL")}");
            return ok;
        }

        /// <summary>BFS over legal moves (8-directional, honoring corner-cut rules) from start to goal.</summary>
        private static List<Direction> FindPath(DungeonMap map, Point start, Point goal)
        {
            Direction[] allDirections =
            {
                Direction.North, Direction.NorthEast, Direction.East, Direction.SouthEast,
                Direction.South, Direction.SouthWest, Direction.West, Direction.NorthWest
            };

            var cameFrom = new Dictionary<Point, (Point parent, Direction step)>();
            var queue = new Queue<Point>();
            queue.Enqueue(start);
            cameFrom[start] = (start, Direction.None);

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                if (current == goal) break;

                foreach (Direction dir in allDirections)
                {
                    if (!map.CanMove(current, dir)) continue;
                    Point next = current + dir.ToOffset();
                    if (cameFrom.ContainsKey(next)) continue;
                    cameFrom[next] = (current, dir);
                    queue.Enqueue(next);
                }
            }

            if (!cameFrom.ContainsKey(goal)) return null;

            var path = new List<Direction>();
            Point node = goal;
            while (node != start)
            {
                (Point parent, Direction step) = cameFrom[node];
                path.Add(step);
                node = parent;
            }
            path.Reverse();
            return path;
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
                        _ => map.GetTile(p).Type switch
                        {
                            TileType.Stairs => '>',
                            TileType.Floor => '.',
                            _ => '#'
                        }
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
