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
    /// Wild Pokémon AI: attack the player when a usable move can reach them,
    /// chase when they're within detection range, wander lazily otherwise.
    /// </summary>
    public class Enemy : Actor
    {
        public Enemy(Point gridPosition, SpeciesDefinition species, int level)
            : base(gridPosition, species, level) { }

        /// <summary>
        /// Decide this enemy's action for the turn. <paramref name="isTileFree"/>
        /// answers whether a target tile will be unoccupied at the end of the turn
        /// (the TurnController tracks reservations).
        /// </summary>
        public TurnAction DecideAction(DungeonMap map, Player player, Func<Point, bool> isTileFree, Rng rng)
        {
            // Attack if any move with PP can reach the player from here.
            for (int i = 0; i < Moves.Count; i++)
            {
                if (!Moves[i].HasPP) continue;
                if (Targeting.InRange(map, this, player, Moves[i].Move, out Direction attackDir))
                {
                    Facing = attackDir;
                    return new AttackAction(i);
                }
            }

            int distance = Chebyshev(GridPosition, player.GridPosition);

            if (distance <= GameConstants.Instance.DetectionRange)
            {
                Direction step = BestStepToward(map, player.GridPosition, isTileFree);
                if (step != Direction.None) return new MoveAction(step);
                return new WaitAction();
            }

            // Out of range: lazy wander.
            if (rng.Chance(0.5f))
            {
                Direction wander = RandomValidDirection(map, isTileFree, rng);
                if (wander != Direction.None) return new MoveAction(wander);
            }
            return new WaitAction();
        }

        /// <summary>
        /// Greedy chase: pick the legal step that gets closest to the player.
        /// Returns None when no step improves on standing still.
        /// </summary>
        private Direction BestStepToward(DungeonMap map, Point target, Func<Point, bool> isTileFree)
        {
            Direction best = Direction.None;
            int bestDistance = Chebyshev(GridPosition, target);
            int bestManhattan = Manhattan(GridPosition, target);

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
            }

            return best;
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
