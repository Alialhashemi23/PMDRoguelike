using Microsoft.Xna.Framework;
using PMDRoguelike.Core;
using PMDRoguelike.Data;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using System;

namespace PMDRoguelike.Combat
{
    /// <summary>
    /// Shared targeting rules for melee and straight-line moves. Walls block lines
    /// (including PMD corner rules for diagonals) and the first actor hit is the target.
    /// </summary>
    public static class Targeting
    {
        /// <summary>Find whatever the attacker's move would hit along its current facing.</summary>
        public static Actor FindTarget(DungeonMap map, Actor attacker, MoveDefinition move)
        {
            Direction dir = attacker.Facing;
            if (dir == Direction.None) return null;

            int reach = move.Range == MoveRange.Melee ? 1 : Math.Max(1, move.Distance);
            Point current = attacker.GridPosition;

            for (int step = 0; step < reach; step++)
            {
                if (!map.CanMove(current, dir)) return null;
                current += dir.ToOffset();
                Actor hit = map.GetActorAt(current);
                if (hit != null) return hit;
            }

            return null;
        }

        /// <summary>
        /// Can the attacker hit the target with this move from where it stands?
        /// Outputs the facing direction to use. Requires exact alignment for line moves.
        /// </summary>
        public static bool InRange(DungeonMap map, Actor attacker, Actor target, MoveDefinition move, out Direction direction)
        {
            direction = Direction.None;

            int dx = target.GridPosition.X - attacker.GridPosition.X;
            int dy = target.GridPosition.Y - attacker.GridPosition.Y;
            bool aligned = dx == 0 || dy == 0 || Math.Abs(dx) == Math.Abs(dy);
            if (!aligned) return false;

            int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
            int reach = move.Range == MoveRange.Melee ? 1 : Math.Max(1, move.Distance);
            if (distance == 0 || distance > reach) return false;

            direction = DirectionExtensions.FromOffset(Math.Sign(dx), Math.Sign(dy));

            // Walk the ray: walls (and corner cuts) block, and another actor in the way
            // means this target can't be hit.
            Point current = attacker.GridPosition;
            for (int step = 0; step < distance; step++)
            {
                if (!map.CanMove(current, direction)) return false;
                current += direction.ToOffset();
                Actor occupant = map.GetActorAt(current);
                if (occupant != null) return occupant == target;
            }

            return false;
        }
    }
}
