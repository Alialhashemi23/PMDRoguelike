using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace PMDRoguelike.Dungeon
{
    /// <summary>
    /// Static definition of one dungeon in the run, authored in Content/Data/Dungeons.json.
    /// Adding or rebalancing a dungeon should never require a code change.
    /// </summary>
    public class DungeonDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Floors { get; set; }

        /// <summary>Floor dimensions; 0 falls back to GameConstants.WorldGeneration.</summary>
        public int FloorWidth { get; set; }
        public int FloorHeight { get; set; }

        /// <summary>Placeholder palette (hex colors) until real tilesets arrive in the sprite phase.</summary>
        public string WallColor { get; set; }
        public string FloorColor { get; set; }

        // Which species spawn here and at what levels.
        public LevelRange EnemyLevels { get; set; } = new LevelRange { Min = 1, Max = 3 };
        public List<string> EnemySpecies { get; set; } = new List<string>();

        /// <summary>The boss guarding this dungeon's final floor.</summary>
        public BossDefinition Boss { get; set; }
    }

    public class BossDefinition
    {
        public string Species { get; set; }
        public int Level { get; set; }
        /// <summary>Display title, e.g. "Overgrown Guardian Ivysaur".</summary>
        public string Title { get; set; }
    }

    public class LevelRange
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class DungeonDatabase
    {
        public List<DungeonDefinition> Dungeons { get; set; }
    }

    /// <summary>
    /// Loads the dungeon list for a run from Content/Data/Dungeons.json,
    /// with hardcoded fallbacks if the file is missing or malformed.
    /// </summary>
    public static class DungeonRegistry
    {
        public static List<DungeonDefinition> Load()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Content", "Data", "Dungeons.json");
            try
            {
                string json = File.ReadAllText(path);
                DungeonDatabase db = JsonConvert.DeserializeObject<DungeonDatabase>(json);
                if (db?.Dungeons == null || db.Dungeons.Count == 0)
                    throw new InvalidDataException("no dungeons defined");
                return db.Dungeons;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dungeons ({path}): {ex.Message} — using defaults.");
                return CreateDefaults();
            }
        }

        private static List<DungeonDefinition> CreateDefaults() => new List<DungeonDefinition>
        {
            new DungeonDefinition
            {
                Id = "verdant-hollow", Name = "Verdant Hollow", Floors = 4,
                FloorWidth = 46, FloorHeight = 32,
                WallColor = "#2F4A33", FloorColor = "#A9C48E",
                EnemyLevels = new LevelRange { Min = 2, Max = 5 }
            },
            new DungeonDefinition
            {
                Id = "ember-caverns", Name = "Ember Caverns", Floors = 4,
                FloorWidth = 52, FloorHeight = 36,
                WallColor = "#4A2E28", FloorColor = "#C9A184",
                EnemyLevels = new LevelRange { Min = 6, Max = 10 }
            },
            new DungeonDefinition
            {
                Id = "tempest-spire", Name = "Tempest Spire", Floors = 5,
                FloorWidth = 56, FloorHeight = 40,
                WallColor = "#33334F", FloorColor = "#9FA6C4",
                EnemyLevels = new LevelRange { Min = 11, Max = 16 }
            }
        };
    }
}
