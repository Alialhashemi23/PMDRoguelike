using System;
using System.Collections.Generic;

namespace PMDRoguelike.Data
{
    /// <summary>
    /// The 18-type effectiveness chart. Authored in Content/Data/TypeChart.json as a
    /// sparse map (only non-neutral matchups); everything else is 1x.
    /// </summary>
    public class TypeChart
    {
        private readonly float[,] _multipliers;

        public TypeChart(Dictionary<string, Dictionary<string, float>> sparse)
        {
            int count = Enum.GetValues<PokemonType>().Length;
            _multipliers = new float[count, count];
            for (int a = 0; a < count; a++)
                for (int d = 0; d < count; d++)
                    _multipliers[a, d] = 1f;

            foreach ((string attacker, Dictionary<string, float> row) in sparse)
            {
                PokemonType attackType = PokemonTypeExtensions.Parse(attacker);
                foreach ((string defender, float multiplier) in row)
                {
                    PokemonType defendType = PokemonTypeExtensions.Parse(defender);
                    _multipliers[(int)attackType, (int)defendType] = multiplier;
                }
            }
        }

        public float Effectiveness(PokemonType attackType, PokemonType defendType) =>
            _multipliers[(int)attackType, (int)defendType];

        /// <summary>Combined multiplier against a (possibly dual-typed) defender.</summary>
        public float Effectiveness(PokemonType attackType, IReadOnlyList<PokemonType> defenderTypes)
        {
            float total = 1f;
            foreach (PokemonType defendType in defenderTypes)
                total *= Effectiveness(attackType, defendType);
            return total;
        }
    }
}
