using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMDRoguelike.Constants
{
    public class GameConstants
    {
        private static GameConstants _instance;
        private GameConstantsData _constants;

        public static GameConstants Instance => _instance ??= new GameConstants();

        private GameConstants() { }

        public void LoadConstants(ContentManager content)
        {
            try
            {
                // Load the JSON file using Content Manager
                string json = File.ReadAllText(
                    Path.Combine(content.RootDirectory, "GameConstants.json"));

                _constants = JsonConvert.DeserializeObject<GameConstantsData>(json);
                Console.WriteLine("Game constants loaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading game constants: {ex.Message}");
                // Use default values if loading fails
                _constants = CreateDefaultConstants();
            }
        }

        private GameConstantsData CreateDefaultConstants()
        {
            // Return hardcoded defaults as fallback
            return new GameConstantsData
            {
                Graphics = new GraphicsConstants { TileSize = 32 },
                // ... etc, abbreviated for brevity
            };
        }

        // Convenience getters
        public int TileSize => _constants.Graphics.TileSize;
        public float PlayerMovementSpeed => _constants.GameMechanics.Movement.PlayerMovementSpeed;
        public int MaxRoomsPerFloor => _constants.WorldGeneration.Dungeons.MaxRoomsPerFloor;
        // Add more getters as needed
    }

    public class GameConstantsData
    {
        public GraphicsConstants Graphics { get; set; }
        public GameMechanicsConstants GameMechanics { get; set; }
        public WorldGenerationConstants WorldGeneration { get; set; }
        public CombatConstants Combat { get; set; }
        public InputConstants Input { get; set; }
        public AssetsConstants Assets { get; set; }
        public AIConstants AI { get; set; }
        public DebugConstants Debug { get; set; }
    }

    public class GraphicsConstants
    {
        public int VirtualResolutionWidth { get; set; }
        public int VirtualResolutionHeight { get; set; }
        public int DefaultWindowWidth { get; set; }
        public int DefaultWindowHeight { get; set; }
        public int TileSize { get; set; }
        public int PokemonSpriteSize { get; set; }
        public int ItemSpriteSize { get; set; }
    }

    public class GameMechanicsConstants
    {
        public MovementConstants Movement { get; set; }
        public TurnConstants Turns { get; set; }
        public ExperienceConstants Experience { get; set; }
    }

    public class MovementConstants
    {
        public float PlayerMovementSpeed { get; set; }
        public float EnemyMovementSpeed { get; set; }
    }

    public class TurnConstants
    {
        public float TurnDurationMs { get; set; }
    }

    public class ExperienceConstants
    {
        public int BaseExpRequired { get; set; }
        public float ExpScaleFactor { get; set; }
        public int MaxLevel { get; set; }
    }

    public class WorldGenerationConstants
    {
        public DungeonConstants Dungeons { get; set; }
        public SpawningConstants Spawning { get; set; }
    }

    public class DungeonConstants
    {
        public int MinRoomSize { get; set; }
        public int MaxRoomSize { get; set; }
        public int MaxRoomsPerFloor { get; set; }
        public int MinCorridorLength { get; set; }
        public int MaxCorridorLength { get; set; }
    }

    public class SpawningConstants
    {
        public float EnemySpawnRate { get; set; }
        public float ItemSpawnRate { get; set; }
        public float TrapSpawnRate { get; set; }
    }

    public class CombatConstants
    {
        public BaseStatsConstants BaseStats { get; set; }
        public TypeEffectivenessConstants TypeEffectiveness { get; set; }
        public StatusEffectsConstants StatusEffects { get; set; }
    }

    public class BaseStatsConstants
    {
        public int BaseAttackDamage { get; set; }
        public int BaseDefense { get; set; }
        public int BaseHP { get; set; }
    }

    public class TypeEffectivenessConstants
    {
        public float SuperEffective { get; set; }
        public float NotVeryEffective { get; set; }
        public float NoEffect { get; set; }
        public float Neutral { get; set; }
    }

    public class StatusEffectsConstants
    {
        public int PoisonDuration { get; set; }
        public int ParalyzeDuration { get; set; }
        public int SleepDuration { get; set; }
        public int BurnDuration { get; set; }
    }

    public class InputConstants
    {
        public KeyBindingsConstants KeyBindings { get; set; }
    }

    public class KeyBindingsConstants
    {
        public string Up { get; set; }
        public string Down { get; set; }
        public string Left { get; set; }
        public string Right { get; set; }
        public string Confirm { get; set; }
        public string Cancel { get; set; }
        public string Menu { get; set; }
    }

    public class AssetsConstants
    {
        public PathsConstants Paths { get; set; }
    }

    public class PathsConstants
    {
        public string Sprites { get; set; }
        public string Audio { get; set; }
        public string Fonts { get; set; }
        public string Maps { get; set; }
        public string Data { get; set; }
    }

    public class AIConstants
    {
        public AIParametersConstants Parameters { get; set; }
    }

    public class AIParametersConstants
    {
        public int DetectionRange { get; set; }
        public int AttackRange { get; set; }
        public float ThinkDelay { get; set; }
    }

    public class DebugConstants
    {
        public DebugSettingsConstants Settings { get; set; }
    }

    public class DebugSettingsConstants
    {
        public bool ShowCollisionBoxes { get; set; }
        public bool ShowFPS { get; set; }
        public bool EnableCheats { get; set; }
    }
}
