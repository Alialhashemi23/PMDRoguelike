using System;

namespace PMDRoguelike.Data
{
    /// <summary>The six core stats. Used both for species base stats and computed level stats.</summary>
    public struct StatBlock
    {
        public int HP;
        public int Attack;
        public int Defense;
        public int SpAttack;
        public int SpDefense;
        public int Speed;

        /// <summary>
        /// Compute the stats a Pokémon has at a given level from its species base stats,
        /// using the mainline formulas without IVs/EVs/natures (roguelike simplification).
        /// </summary>
        public static StatBlock AtLevel(StatBlock baseStats, int level) => new StatBlock
        {
            HP = 2 * baseStats.HP * level / 100 + level + 10,
            Attack = OtherStat(baseStats.Attack, level),
            Defense = OtherStat(baseStats.Defense, level),
            SpAttack = OtherStat(baseStats.SpAttack, level),
            SpDefense = OtherStat(baseStats.SpDefense, level),
            Speed = OtherStat(baseStats.Speed, level)
        };

        private static int OtherStat(int baseStat, int level) => 2 * baseStat * level / 100 + 5;
    }
}
