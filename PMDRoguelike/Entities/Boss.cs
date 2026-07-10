using Microsoft.Xna.Framework;
using PMDRoguelike.Core;
using PMDRoguelike.Data;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Turns;
using PMDRoguelike.UI;
using System;
using System.Collections.Generic;

namespace PMDRoguelike.Entities
{
    /// <summary>
    /// A dungeon boss: bulkier than its species (1.5x HP), aware of the player from
    /// the start, and running a simple pattern — summons two minions the first time
    /// it drops below 60% HP, and enrages (+30% offenses) below 30%.
    /// </summary>
    public class Boss : Enemy
    {
        private const float HpMultiplier = 1.5f;
        private const float EnrageMultiplier = 1.3f;
        private const float SummonThreshold = 0.60f;
        private const float EnrageThreshold = 0.30f;
        private const int SummonCount = 2;

        private readonly IReadOnlyList<string> _minionSpecies;
        private readonly int _minionLevel;
        private bool _summoned;
        private bool _enraged;

        /// <summary>Display title, e.g. "Overgrown Guardian Ivysaur".</summary>
        public string Title { get; }

        public override string DisplayName => Title ?? base.DisplayName;

        public Boss(Point gridPosition, SpeciesDefinition species, int level, string title,
            IReadOnlyList<string> minionSpecies, int minionLevel)
            : base(gridPosition, species, level)
        {
            Title = title;
            _minionSpecies = minionSpecies is { Count: > 0 } ? minionSpecies : new[] { "rattata" };
            _minionLevel = Math.Max(1, minionLevel);
            Alerted = true;
            RefreshStats(); // apply the boss HP multiplier via ComputeStats
        }

        protected override StatBlock ComputeStats()
        {
            StatBlock stats = base.ComputeStats();
            stats.HP = (int)(stats.HP * HpMultiplier);
            if (_enraged)
            {
                stats.Attack = (int)(stats.Attack * EnrageMultiplier);
                stats.SpAttack = (int)(stats.SpAttack * EnrageMultiplier);
            }
            return stats;
        }

        public override TurnAction DecideAction(DungeonMap map, Player player, Func<Point, bool> isTileFree, Rng rng,
            MessageLog log)
        {
            float hpFraction = (float)CurrentHP / Stats.HP;

            // One-time summon: costs the boss its turn.
            if (!_summoned && hpFraction <= SummonThreshold)
            {
                _summoned = true;
                int summoned = SummonMinions(map, rng);
                log.Add(summoned > 0
                    ? $"{DisplayName} calls for backup!"
                    : $"{DisplayName} roars in fury!");
                AudioCues.Post("boss");
                return new WaitAction();
            }

            // One-time enrage: free action, then fight on.
            if (!_enraged && hpFraction <= EnrageThreshold)
            {
                _enraged = true;
                RefreshStats();
                log.Add($"{DisplayName} is enraged! Its attacks sharpen!");
                AudioCues.Post("boss");
            }

            return base.DecideAction(map, player, isTileFree, rng, log);
        }

        private int SummonMinions(DungeonMap map, Rng rng)
        {
            Point[] offsets =
            {
                new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
                new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)
            };

            int summoned = 0;
            foreach (Point offset in offsets)
            {
                if (summoned >= SummonCount) break;
                Point p = GridPosition + offset;
                if (!map.IsWalkable(p) || map.IsOccupied(p)) continue;

                var minion = new Enemy(p, GameData.GetSpecies(_minionSpecies[rng.Next(_minionSpecies.Count)]), _minionLevel);
                map.Actors.Add(minion);
                summoned++;
            }
            return summoned;
        }
    }
}
