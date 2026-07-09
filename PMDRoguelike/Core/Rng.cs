using System;

namespace PMDRoguelike.Core
{
    /// <summary>
    /// Seedable random wrapper so dungeon generation and AI are reproducible.
    /// </summary>
    public class Rng
    {
        private readonly Random _random;

        public int Seed { get; }

        public Rng() : this(Environment.TickCount) { }

        public Rng(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        /// <summary>Inclusive min, exclusive max.</summary>
        public int Next(int min, int max) => _random.Next(min, max);

        public int Next(int maxExclusive) => _random.Next(maxExclusive);

        public float NextFloat() => (float)_random.NextDouble();

        /// <summary>Returns true with the given probability (0..1).</summary>
        public bool Chance(float probability) => _random.NextDouble() < probability;

        public T Pick<T>(System.Collections.Generic.IReadOnlyList<T> items) => items[_random.Next(items.Count)];
    }
}
