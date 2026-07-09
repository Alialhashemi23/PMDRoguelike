using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PMDRoguelike.Data
{
    /// <summary>
    /// Central registry for all authored game data (species, moves, type chart),
    /// loaded once at startup from Content/Data/*.json. Adding content means editing
    /// JSON, never code.
    /// </summary>
    public static class GameData
    {
        private static Dictionary<string, SpeciesDefinition> _species;
        private static Dictionary<string, MoveDefinition> _moves;

        public static TypeChart TypeChart { get; private set; }
        public static bool IsLoaded => _species != null;

        /// <summary>Built-in fallback move used when all PP is gone. Not part of Moves.json.</summary>
        public static MoveDefinition Struggle { get; } = new MoveDefinition
        {
            Id = "struggle",
            Name = "Struggle",
            Type = PokemonType.Normal,
            Category = MoveCategory.Physical,
            Power = 50,
            Accuracy = 100,
            PP = 0,
            Range = MoveRange.Melee
        };

        public static void Load()
        {
            string dataDir = Path.Combine(AppContext.BaseDirectory, "Content", "Data");

            TypeChart = new TypeChart(LoadJson<Dictionary<string, Dictionary<string, float>>>(
                Path.Combine(dataDir, "TypeChart.json")));

            _moves = LoadMoves(Path.Combine(dataDir, "Moves.json"));
            _species = LoadSpecies(Path.Combine(dataDir, "Species.json"));

            Console.WriteLine($"Game data loaded: {_species.Count} species, {_moves.Count} moves.");
        }

        public static SpeciesDefinition GetSpecies(string id)
        {
            if (_species.TryGetValue(id, out SpeciesDefinition species)) return species;
            throw new KeyNotFoundException($"Unknown species '{id}' — check Species.json and Dungeons.json.");
        }

        public static MoveDefinition GetMove(string id)
        {
            if (id == Struggle.Id) return Struggle;
            if (_moves.TryGetValue(id, out MoveDefinition move)) return move;
            throw new KeyNotFoundException($"Unknown move '{id}' — check Moves.json and learnsets.");
        }

        private static T LoadJson<T>(string path) =>
            JsonConvert.DeserializeObject<T>(File.ReadAllText(path))
            ?? throw new InvalidDataException($"{path} deserialized to null");

        private static Dictionary<string, MoveDefinition> LoadMoves(string path)
        {
            var root = LoadJson<JObject>(path);
            var moves = new Dictionary<string, MoveDefinition>();
            foreach (JToken token in root["moves"] ?? throw new InvalidDataException("Moves.json missing 'moves'"))
            {
                var move = new MoveDefinition
                {
                    Id = (string)token["id"],
                    Name = (string)token["name"],
                    Type = PokemonTypeExtensions.Parse((string)token["type"]),
                    Category = string.Equals((string)token["category"], "physical", StringComparison.OrdinalIgnoreCase)
                        ? MoveCategory.Physical : MoveCategory.Special,
                    Power = (int?)token["power"] ?? 0,
                    Accuracy = (int?)token["accuracy"] ?? 100,
                    PP = (int?)token["pp"] ?? 10,
                    Range = string.Equals((string)token["range"], "line", StringComparison.OrdinalIgnoreCase)
                        ? MoveRange.Line : MoveRange.Melee,
                    Distance = (int?)token["distance"] ?? 1
                };
                moves[move.Id] = move;
            }
            return moves;
        }

        private static Dictionary<string, SpeciesDefinition> LoadSpecies(string path)
        {
            var root = LoadJson<JObject>(path);
            var result = new Dictionary<string, SpeciesDefinition>();
            foreach (JToken token in root["species"] ?? throw new InvalidDataException("Species.json missing 'species'"))
            {
                JToken stats = token["baseStats"] ?? throw new InvalidDataException("species missing baseStats");
                var species = new SpeciesDefinition
                {
                    Id = (string)token["id"],
                    Name = (string)token["name"],
                    Types = (token["types"] ?? new JArray())
                        .Select(t => PokemonTypeExtensions.Parse((string)t)).ToList(),
                    BaseStats = new StatBlock
                    {
                        HP = (int?)stats["hp"] ?? 1,
                        Attack = (int?)stats["attack"] ?? 1,
                        Defense = (int?)stats["defense"] ?? 1,
                        SpAttack = (int?)stats["spAttack"] ?? 1,
                        SpDefense = (int?)stats["spDefense"] ?? 1,
                        Speed = (int?)stats["speed"] ?? 1
                    },
                    ExpYield = (int?)token["expYield"] ?? 50,
                    Color = (string)token["color"] ?? "#FF00FF",
                    Learnset = (token["learnset"] ?? new JArray()).Select(entry => new LearnsetEntry
                    {
                        Level = (int?)entry["level"] ?? 1,
                        Move = (string)entry["move"]
                    }).OrderBy(entry => entry.Level).ToList()
                };

                // Fail fast on typos: every learnset move must exist.
                foreach (LearnsetEntry entry in species.Learnset)
                {
                    if (!_moves.ContainsKey(entry.Move))
                        throw new InvalidDataException($"Species '{species.Id}' references unknown move '{entry.Move}'");
                }

                result[species.Id] = species;
            }
            return result;
        }
    }
}
