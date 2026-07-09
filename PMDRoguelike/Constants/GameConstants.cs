using Newtonsoft.Json;
using System;
using System.IO;

namespace PMDRoguelike.Constants
{
    public class GameConstants
    {
        private static GameConstants _instance;
        private GameConstantsData _constants;

        public static GameConstants Instance => _instance ??= new GameConstants();

        private GameConstants()
        {
            // Always start with valid defaults so getters never null-ref,
            // even if LoadConstants is never called (e.g. in tests).
            _constants = CreateDefaultConstants();
        }

        /// <summary>
        /// Load constants from Constants/GameConstants.json next to the executable.
        /// Falls back to hardcoded defaults if the file is missing or malformed.
        /// </summary>
        public void LoadConstants()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Constants", "GameConstants.json");
            try
            {
                string json = File.ReadAllText(path);
                _constants = JsonConvert.DeserializeObject<GameConstantsData>(json)
                             ?? throw new InvalidDataException("GameConstants.json deserialized to null.");
                Console.WriteLine($"Game constants loaded from {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading game constants ({path}): {ex.Message} — using defaults.");
                _constants = CreateDefaultConstants();
            }
        }

        private static GameConstantsData CreateDefaultConstants() => new GameConstantsData
        {
            Graphics = new GraphicsConstants
            {
                VirtualResolutionWidth = 1280,
                VirtualResolutionHeight = 720,
                DefaultWindowWidth = 1280,
                DefaultWindowHeight = 720,
                TileSize = 32,
                PokemonSpriteSize = 32,
                ItemSpriteSize = 16
            },
            GameMechanics = new GameMechanicsConstants
            {
                Movement = new MovementConstants
                {
                    PlayerMovementSpeed = 200f,
                    EnemyMovementSpeed = 150f,
                    SlideDurationMs = 180f,
                    InputDelayMs = 60f
                },
                Turns = new TurnConstants { TurnDurationMs = 500f },
                Experience = new ExperienceConstants { BaseExpRequired = 100, ExpScaleFactor = 1.5f, MaxLevel = 100 }
            },
            WorldGeneration = new WorldGenerationConstants
            {
                Dungeons = new DungeonConstants
                {
                    FloorWidth = 50,
                    FloorHeight = 35,
                    MinRoomSize = 5,
                    MaxRoomSize = 15,
                    MaxRoomsPerFloor = 10,
                    MinCorridorLength = 1,
                    MaxCorridorLength = 10
                },
                Spawning = new SpawningConstants
                {
                    EnemySpawnRate = 0.3f,
                    ItemSpawnRate = 0.1f,
                    TrapSpawnRate = 0.05f,
                    MinEnemiesPerFloor = 3,
                    MaxEnemiesPerFloor = 5
                }
            },
            Combat = new CombatConstants
            {
                BaseStats = new BaseStatsConstants { BaseAttackDamage = 10, BaseDefense = 5, BaseHP = 50 },
                TypeEffectiveness = new TypeEffectivenessConstants
                {
                    SuperEffective = 2f,
                    NotVeryEffective = 0.5f,
                    NoEffect = 0f,
                    Neutral = 1f
                },
                StatusEffects = new StatusEffectsConstants
                {
                    PoisonDuration = 5,
                    ParalyzeDuration = 3,
                    SleepDuration = 3,
                    BurnDuration = 5
                }
            },
            Input = new InputConstants
            {
                KeyBindings = new KeyBindingsConstants
                {
                    Up = "Up",
                    Down = "Down",
                    Left = "Left",
                    Right = "Right",
                    Confirm = "Z",
                    Cancel = "X",
                    Menu = "C"
                }
            },
            Assets = new AssetsConstants
            {
                Paths = new PathsConstants
                {
                    Sprites = "Sprites/",
                    Audio = "Audio/",
                    Fonts = "Fonts/",
                    Maps = "Maps/",
                    Data = "Data/"
                }
            },
            AI = new AIConstants
            {
                Parameters = new AIParametersConstants { DetectionRange = 5, AttackRange = 1, ThinkDelay = 0.2f }
            },
            Debug = new DebugConstants
            {
                Settings = new DebugSettingsConstants { ShowCollisionBoxes = false, ShowFPS = true, EnableCheats = false }
            }
        };

        /// <summary>Full constants tree for systems that need direct access.</summary>
        public GameConstantsData Data => _constants;

        // Convenience getters for the most commonly used values
        public int TileSize => _constants.Graphics.TileSize;
        public int WindowWidth => _constants.Graphics.DefaultWindowWidth;
        public int WindowHeight => _constants.Graphics.DefaultWindowHeight;
        public float SlideDurationMs => _constants.GameMechanics.Movement.SlideDurationMs;
        public float InputDelayMs => _constants.GameMechanics.Movement.InputDelayMs;
        public int DetectionRange => _constants.AI.Parameters.DetectionRange;
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
        /// <summary>How long the tile-to-tile slide animation lasts.</summary>
        public float SlideDurationMs { get; set; }
        /// <summary>Grace period after a direction key is pressed, so diagonals register cleanly.</summary>
        public float InputDelayMs { get; set; }
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
        public int FloorWidth { get; set; }
        public int FloorHeight { get; set; }
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
        public int MinEnemiesPerFloor { get; set; }
        public int MaxEnemiesPerFloor { get; set; }
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
