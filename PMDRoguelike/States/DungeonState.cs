using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;
using PMDRoguelike.Run;
using PMDRoguelike.Turns;

namespace PMDRoguelike.States
{
    /// <summary>
    /// The core gameplay state: an active run through the dungeons.
    /// Owns the current floor, turn controller, camera, and run progression.
    /// </summary>
    public class DungeonState : GameState
    {
        private RunManager _run;
        private DungeonMap _map;
        private Player _player;
        private TurnController _turns;
        private Camera _camera;
        private DungeonRenderer _renderer;
        private HudRenderer _hud;

        public DungeonState(PMDRogueGame game) : base(game) { }

        public override void Enter()
        {
            _renderer = new DungeonRenderer(Game.GameContent);
            _hud = new HudRenderer(Game.GameContent.LoadFont("Default"));
            _run = new RunManager(DungeonRegistry.Load());
            BuildFloor();
        }

        /// <summary>Generate the current floor of the current dungeon and spawn actors.</summary>
        private void BuildFloor()
        {
            DungeonDefinition dungeon = _run.CurrentDungeon;
            ApplyPalette(dungeon);

            GeneratedFloor floor = new DungeonGenerator(Game.Rng).Generate(dungeon);
            _map = floor.Map;

            _player = new Player(floor.PlayerSpawn);
            _map.Actors.Add(_player);
            foreach (Point spawn in floor.EnemySpawns)
            {
                _map.Actors.Add(new Enemy(spawn));
            }

            _turns = new TurnController(_map, _player, Game.Rng);
            _camera = new Camera();
        }

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

            // Debug helpers until later phases wire these properly:
            // R regenerates the current floor, F9 fakes a defeat (combat arrives in Phase 3).
            if (keyboard.WasKeyJustPressed(Keys.R)) BuildFloor();
            if (keyboard.WasKeyJustPressed(Keys.F9))
            {
                Game.States.ChangeState(new GameOverState(Game));
                return;
            }

            _turns.Update(gameTime);

            if (CanDescend() && (keyboard.WasKeyJustPressed(Keys.Enter) || keyboard.WasKeyJustPressed(Keys.Z)))
            {
                Descend();
                return;
            }

            float halfTile = GameConstants.Instance.TileSize / 2f;
            _camera.Update(_player.RenderPosition + new Vector2(halfTile, halfTile),
                (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        private bool CanDescend() =>
            _turns.Phase == TurnPhase.AwaitingInput &&
            !_player.IsAnimating &&
            _player.GridPosition == _map.StairsPosition;

        private void Descend()
        {
            _run.AddTurns(_turns.TurnCount);

            AdvanceResult result = _run.Advance();
            if (result == AdvanceResult.Victory)
            {
                Game.States.ChangeState(new VictoryState(Game, _run.TotalTurns));
                return;
            }

            BuildFloor();
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch spriteBatch = Game.SpriteBatch;
            var viewport = Game.GraphicsDevice.Viewport;

            Matrix view = _camera.GetViewMatrix(viewport.Width, viewport.Height);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: view);
            _renderer.Draw(spriteBatch, _map);
            spriteBatch.End();

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _hud.Draw(spriteBatch, _run, _turns.TurnCount, CanDescend(), viewport.Width, viewport.Height);
            spriteBatch.End();
        }
    }
}
