using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Data;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;
using PMDRoguelike.Run;
using PMDRoguelike.Turns;
using PMDRoguelike.UI;

namespace PMDRoguelike.States
{
    /// <summary>
    /// The core gameplay state: an active run through the dungeons.
    /// Owns the current floor, turn controller, camera, and run progression.
    /// </summary>
    public class DungeonState : GameState
    {
        // Starter species until the selection screen arrives in Phase 8.
        private const string StarterSpecies = "charmander";
        private const int StarterLevel = 5;

        private RunManager _run;
        private DungeonMap _map;
        private Player _player;
        private TurnController _turns;
        private Camera _camera;
        private DungeonRenderer _renderer;
        private HudRenderer _hud;
        private MessageLog _log;
        private MoveLearnPrompt _learnPrompt;

        public DungeonState(PMDRogueGame game) : base(game) { }

        public override void Enter()
        {
            _renderer = new DungeonRenderer(Game.GameContent);
            _hud = new HudRenderer(Game.GameContent);
            _log = new MessageLog();
            _run = new RunManager(DungeonRegistry.Load());
            BuildFloor();
            _log.Add($"Entered {_run.CurrentDungeon.Name}!");
        }

        /// <summary>Generate the current floor of the current dungeon and (re)spawn actors.</summary>
        private void BuildFloor()
        {
            DungeonDefinition dungeon = _run.CurrentDungeon;
            ApplyPalette(dungeon);

            GeneratedFloor floor = new DungeonGenerator(Game.Rng).Generate(dungeon);
            _map = floor.Map;

            // The player persists across floors (level, HP, PP); only the floor is new.
            if (_player == null)
            {
                SpeciesDefinition starter = GameData.GetSpecies(StarterSpecies);
                _player = new Player(floor.PlayerSpawn, starter, StarterLevel);
                RegisterSpeciesColor(starter);
            }
            else
            {
                _player.SnapTo(floor.PlayerSpawn);
            }
            _map.Actors.Add(_player);

            foreach (Point spawn in floor.EnemySpawns)
            {
                string speciesId = dungeon.EnemySpecies.Count > 0
                    ? Game.Rng.Pick(dungeon.EnemySpecies)
                    : "rattata";
                SpeciesDefinition species = GameData.GetSpecies(speciesId);
                RegisterSpeciesColor(species);

                _map.Actors.Add(new Enemy(spawn, species, ScaledEnemyLevel(dungeon)));
            }

            SpawnFloorItems(floor);
            EconomyPopulator.Populate(_map, floor.PlayerSpawn, Game.Rng, _run.Depth);

            _turns = new TurnController(_map, _player, Game.Rng, _log);
            _camera = new Camera();

            // Per-floor item state resets (Choice Band unlock, Focus Sash charges).
            _player.Inventory.OnFloorStart(_turns.ItemContext);
        }

        /// <summary>Scatter 1-2 free items on the floor (chests/shops arrive in Phase 6).</summary>
        private void SpawnFloorItems(GeneratedFloor floor)
        {
            int count = Game.Rng.Next(1, 3);
            int attempts = count * 20;
            while (count > 0 && attempts-- > 0)
            {
                Microsoft.Xna.Framework.Rectangle room = _map.Rooms[Game.Rng.Next(_map.Rooms.Count)];
                var p = new Point(Game.Rng.Next(room.Left, room.Right), Game.Rng.Next(room.Top, room.Bottom));
                if (p == floor.PlayerSpawn || p == _map.StairsPosition) continue;
                if (_map.IsOccupied(p) || _map.GroundItemAt(p) != null) continue;

                _map.GroundItems.Add(new Items.GroundItem(p, Items.ItemRegistry.Roll(Game.Rng)));
                count--;
            }
        }

        /// <summary>
        /// Enemy level climbs through the dungeon's range as the player descends its
        /// floors (±1 jitter), so early floors stay gentle and the last floor bites.
        /// </summary>
        private int ScaledEnemyLevel(DungeonDefinition dungeon)
        {
            float progress = dungeon.Floors > 1 ? (_run.FloorNumber - 1f) / (dungeon.Floors - 1f) : 1f;
            int mid = (int)System.Math.Round(dungeon.EnemyLevels.Min +
                (dungeon.EnemyLevels.Max - dungeon.EnemyLevels.Min) * progress);
            int level = mid + Game.Rng.Next(-1, 2);
            return System.Math.Clamp(level, dungeon.EnemyLevels.Min, dungeon.EnemyLevels.Max);
        }

        private void RegisterSpeciesColor(SpeciesDefinition species) =>
            Game.GameContent.RegisterSolid($"species.{species.Id}", ColorUtil.FromHex(species.Color));

        /// <summary>Placeholder tile colors per dungeon, until real tilesets in the sprite phase.</summary>
        private void ApplyPalette(DungeonDefinition dungeon)
        {
            Game.GameContent.RegisterSolid("tile.wall", ColorUtil.FromHex(dungeon.WallColor), overwrite: true);
            Game.GameContent.RegisterSolid("tile.floor", ColorUtil.FromHex(dungeon.FloorColor), overwrite: true);
            Game.GameContent.RegisterSolid("tile.stairs", new Color(96, 134, 222), overwrite: true);
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardManager keyboard = KeyboardManager.Instance;

            // Modal move-learn prompt blocks everything else.
            if (_learnPrompt != null)
            {
                if (_learnPrompt.Update(keyboard)) _learnPrompt = null;
                return;
            }

            // Debug helper until stairs are the only path: R regenerates the current floor.
            if (keyboard.WasKeyJustPressed(Keys.R)) BuildFloor();

            _turns.Update(gameTime);

            if (_turns.PlayerDefeated)
            {
                Game.States.ChangeState(new GameOverState(Game,
                    $"Defeated in {_run.CurrentDungeon.Name} F{_run.FloorNumber} at Lv.{_player.Level}."));
                return;
            }

            if (_player.PendingMoveLearns.Count > 0)
            {
                _learnPrompt = new MoveLearnPrompt(_player, _log);
                return;
            }

            if (CanInteract() && (keyboard.WasKeyJustPressed(Keys.Enter) || keyboard.WasKeyJustPressed(Keys.Z)))
            {
                HandleInteraction();
                return;
            }

            float halfTile = GameConstants.Instance.TileSize / 2f;
            _camera.Update(_player.RenderPosition + new Vector2(halfTile, halfTile),
                (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        private bool CanInteract() => _turns.Phase == TurnPhase.AwaitingInput && !_player.IsAnimating;

        /// <summary>Shop purchase and chest opening take precedence over the stairs.</summary>
        private void HandleInteraction()
        {
            Point pos = _player.GridPosition;

            Items.ShopItem shopItem = _map.ShopItemAt(pos);
            if (shopItem != null)
            {
                Items.Economy.TryBuy(_player, _map, shopItem, _log);
                return;
            }

            Items.Chest chest = _map.ChestAt(pos);
            if (chest != null)
            {
                Items.Economy.TryOpenChest(_player, _map, chest, _log, Game.Rng, _run.Depth);
                return;
            }

            if (pos == _map.StairsPosition) Descend();
        }

        /// <summary>Contextual prompt for the tile the player is standing on.</summary>
        private string CurrentPrompt()
        {
            if (!CanInteract()) return null;
            Point pos = _player.GridPosition;

            Items.ShopItem shopItem = _map.ShopItemAt(pos);
            if (shopItem != null) return $"Buy {shopItem.Item.Name} — {shopItem.Price} Poké (Enter)";

            Items.Chest chest = _map.ChestAt(pos);
            if (chest != null) return $"Open chest — {chest.Price} Poké (Enter)";

            if (pos == _map.StairsPosition) return "Press Enter to descend";
            return null;
        }

        private void Descend()
        {
            _run.AddTurns(_turns.TurnCount);

            AdvanceResult result = _run.Advance();
            if (result == AdvanceResult.Victory)
            {
                Game.States.ChangeState(new VictoryState(Game, _run.TotalTurns));
                return;
            }

            // A short rest at the stairs: PP refills, HP carries over.
            _player.RestoreAllPP();
            BuildFloor();

            _log.Add(result == AdvanceResult.NextDungeon
                ? $"Entered {_run.CurrentDungeon.Name}!"
                : $"{_run.CurrentDungeon.Name} — floor {_run.FloorNumber}.");
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = Game.SpriteBatch;
            var viewport = Game.GraphicsDevice.Viewport;
            SpriteFont font = Game.GameContent.LoadFont("Default");
            Texture2D pixel = Game.GameContent.GetTexture("ui.pixel");

            Matrix view = _camera.GetViewMatrix(viewport.Width, viewport.Height);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: view);
            _renderer.Draw(spriteBatch, _map);
            spriteBatch.End();

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _hud.Draw(spriteBatch, _run, _player, _turns.TurnCount, CurrentPrompt(), viewport.Width, viewport.Height);
            _log.Draw(spriteBatch, font, pixel, viewport.Width, viewport.Height);

            if (KeyboardManager.Instance.IsKeyDown(Keys.LeftShift) || KeyboardManager.Instance.IsKeyDown(Keys.RightShift))
            {
                MovePanel.Draw(spriteBatch, font, pixel, _player, viewport.Width);
            }
            else if (KeyboardManager.Instance.IsKeyDown(Keys.Tab))
            {
                InventoryPanel.Draw(spriteBatch, font, pixel, _player, viewport.Width);
            }

            _learnPrompt?.Draw(spriteBatch, font, pixel, viewport.Width, viewport.Height);
            spriteBatch.End();
        }
    }
}
