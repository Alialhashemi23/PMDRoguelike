using System.Collections.Generic;

namespace PMDRoguelike.Data
{
    /// <summary>
    /// Static definition of a Pokémon species, authored in Content/Data/Species.json.
    /// </summary>
    public class SpeciesDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<PokemonType> Types { get; set; } = new List<PokemonType>();
        public StatBlock BaseStats { get; set; }
        /// <summary>Base EXP awarded when this species is defeated (scaled by level).</summary>
        public int ExpYield { get; set; }
        /// <summary>Placeholder tint until real sprites; hex color.</summary>
        public string Color { get; set; }
        /// <summary>Moves learned by leveling, ascending by level.</summary>
        public List<LearnsetEntry> Learnset { get; set; } = new List<LearnsetEntry>();

        public bool HasType(PokemonType type) => Types.Contains(type);
    }

    public class LearnsetEntry
    {
        public int Level { get; set; }
        public string Move { get; set; }
    }
}
