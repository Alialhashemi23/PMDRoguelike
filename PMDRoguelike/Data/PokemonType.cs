using System;

namespace PMDRoguelike.Data
{
    public enum PokemonType
    {
        Normal, Fire, Water, Electric, Grass, Ice,
        Fighting, Poison, Ground, Flying, Psychic, Bug,
        Rock, Ghost, Dragon, Dark, Steel, Fairy
    }

    public static class PokemonTypeExtensions
    {
        public static PokemonType Parse(string name)
        {
            if (Enum.TryParse(name, ignoreCase: true, out PokemonType type)) return type;
            throw new FormatException($"Unknown Pokémon type '{name}'");
        }
    }
}
