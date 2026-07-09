using PMDRoguelike.Core;
using PMDRoguelike.Data;
using PMDRoguelike.Entities;
using System;

namespace PMDRoguelike.Combat
{
    public struct DamageResult
    {
        public int Damage;
        public float Effectiveness;
        public bool IsCritical;
        public bool Missed;
    }

    /// <summary>
    /// Standard mainline Pokémon damage math (no IV/EV/nature/items — those layers
    /// come from the RoR item hooks in Phase 5).
    /// </summary>
    public static class DamageCalculator
    {
        private const float CritChance = 1f / 16f;
        private const float CritMultiplier = 1.5f;
        private const float StabMultiplier = 1.5f;

        public static DamageResult Calculate(Actor attacker, Actor defender, MoveDefinition move, Rng rng)
        {
            if (move.Accuracy < 100 && rng.Next(100) >= move.Accuracy)
                return new DamageResult { Missed = true };

            float effectiveness = GameData.TypeChart.Effectiveness(move.Type, defender.Species.Types);
            if (effectiveness <= 0f)
                return new DamageResult { Damage = 0, Effectiveness = 0f };

            int attackStat = move.Category == MoveCategory.Physical ? attacker.Stats.Attack : attacker.Stats.SpAttack;
            int defenseStat = move.Category == MoveCategory.Physical ? defender.Stats.Defense : defender.Stats.SpDefense;

            bool crit = rng.NextFloat() < CritChance;
            float stab = attacker.Species.HasType(move.Type) ? StabMultiplier : 1f;
            float roll = 0.85f + rng.NextFloat() * 0.15f;
            // Burn halves physical damage output (mainline rule).
            float burn = attacker.StatusType == StatusType.Burn && move.Category == MoveCategory.Physical ? 0.5f : 1f;

            float baseDamage = ((2f * attacker.Level / 5f + 2f) * move.Power * attackStat / defenseStat) / 50f + 2f;
            float total = baseDamage * stab * effectiveness * (crit ? CritMultiplier : 1f) * roll * burn;

            return new DamageResult
            {
                Damage = Math.Max(1, (int)total),
                Effectiveness = effectiveness,
                IsCritical = crit
            };
        }
    }
}
