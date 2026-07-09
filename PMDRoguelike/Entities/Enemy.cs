using Microsoft.Xna.Framework;
using PMDRoguelike.Combat;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Data;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Turns;
using System;

namespace PMDRoguelike.Entities
{
    /// <summary>
    /// Wild Pokémon AI. Enemies are unaware until the player enters their room or
    /// comes into clear line of sight within detection range; once alerted they
    /// chase and attack until the floor ends. Unaware enemies wander lazily.
    /// </summary>
    public class Enemy : Actor
    {
        /// <summary>Whether this enemy has noticed the player (sticky once set).</summary>
        public bool Alerted { get; protected set; }

        public Enemy(Point gridPosition, SpeciesDefinition species, int level)
            : base(gridPosition, species, level) { }

        /// <summary>
        /// Decide this enemy's action for the turn. <paramref name="isTileFree"/>
        /// answers whether a target tile will be unoccupied at the end of the turn
        /// (the TurnController tracks reservations). <paramref name="log"/> lets
        /// special enemies (bosses) narrate their patterns.
        /// </summary>
        public virtual TurnAction DecideAction(DungeonMap map, Player player, Func<Point, bool> isTileFree, Rng rng,
            UI.MessageLog log)
        {
            UpdateAwareness(map, player);

            if (!Alerted)
            {
                // Unaware: lazy wander.
                if (rng.Chance(0.5f))
                {
                    Direction wander = RandomValidDirection(map, isTileFree, rng);
                    if (wander != Direction.None) return new MoveAction(wander);
                }
                return new WaitAction();
            }

            // Attack if any usable move can reach the player from here.
            for (int i = 0; i < Moves.Count; i++)
            {
                if (!Moves[i].HasPP) continue;
                // Don't waste pure status moves on an already-statused player.
                if (Moves[i].Move.Category == MoveCategory.Status && player.StatusType != StatusType.None) continue;
                if (Targeting.InRange(map, this, player, Moves[i].Move, out Direction attackDir))
                {
                    Facing = attackDir;
                    return new AttackAction(i);
                }
            }

            Direction step = BestStepToward(map, player.GridPosition, isTileFree);
            if (step != Direction.None) return new MoveAction(step);
            return new WaitAction();
        }

        private void UpdateAwareness(DungeonMap map, Player player)
        {
            if (Alerted) return;

            // Same room = noticed, regardless of distance.
            Rectangle? room = map.RoomContaining(GridPosition);
            if (room.HasValue && room.Value.Contains(player.GridPosition))
            {
                Alerted = true;
                return;
            }

            // Otherwise: close enough AND actually visible.
            if (Chebyshev(GridPosition, player.GridPosition) <= GameConstants.Instance.DetectionRange &&
                map.HasLineOfSight(GridPosition, player.GridPosition))
            {
                Alerted = true;
            }
        }

        /// <summary>
        /// Greedy chase: pick the legal step that gets closest to the player. When no
        /// step strictly improves (pillar/actor in the way), allow an equal-distance
        /// sidestep so the enemy flows around obstacles instead of freezing.
        /// </summary>
        private Direction BestStepToward(DungeonMap map, Point target, Func<Point, bool> isTileFree)
        {
            Direction best = Direction.None;
            int bestDistance = Chebyshev(GridPosition, target);
            int bestManhattan = Manhattan(GridPosition, target);
            Direction sidestep = Direction.None;

            foreach (Direction dir in AllDirections)
            {
                Point next = GridPosition + dir.ToOffset();
                if (!map.CanMove(GridPosition, dir) || !isTileFree(next)) continue;

                int distance = Chebyshev(next, target);
                int manhattan = Manhattan(next, target);
                // Manhattan tiebreak makes diagonals win over cardinals when both close in.
                if (distance < bestDistance || (distance == bestDistance && manhattan < bestManhattan))
                {
                    best = dir;
                    bestDistance = distance;
                    bestManhattan = manhattan;
                }
                else if (sidestep == Direction.None && distance == Chebyshev(GridPosition, target))
                {
                    sidestep = dir;
                }
            }

            return best != Direction.None ? best : sidestep;
        }

        private Direction RandomValidDirection(DungeonMap map, Func<Point, bool> isTileFree, Rng rng)
        {
            Direction dir = AllDirections[rng.Next(AllDirections.Length)];
            Point next = GridPosition + dir.ToOffset();
            return map.CanMove(GridPosition, dir) && isTileFree(next) ? dir : Direction.None;
        }

        private static readonly Direction[] AllDirections =
        {
            Direction.North, Direction.NorthEast, Direction.East, Direction.SouthEast,
            Direction.South, Direction.SouthWest, Direction.West, Direction.NorthWest
        };

        private static int Chebyshev(Point a, Point b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        private static int Manhattan(Point a, Point b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
}
