using Microsoft.Xna.Framework;
using PMDRoguelike.Core;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;
using System.Collections.Generic;

namespace PMDRoguelike.Turns
{
    public enum TurnPhase
    {
        /// <summary>Waiting for the player to commit an action.</summary>
        AwaitingInput,
        /// <summary>Slide animations from the last turn are playing out.</summary>
        Animating
    }

    /// <summary>
    /// Drives the turn loop: the player commits an action, every enemy then decides
    /// theirs, all resulting moves animate in parallel, and control returns to the
    /// player. Turn resolution itself (ExecuteTurn) is graphics-free so it can run
    /// headlessly in tests.
    /// </summary>
    public class TurnController
    {
        private readonly DungeonMap _map;
        private readonly Player _player;
        private readonly Rng _rng;

        public TurnPhase Phase { get; private set; } = TurnPhase.AwaitingInput;
        public int TurnCount { get; private set; }

        public TurnController(DungeonMap map, Player player, Rng rng)
        {
            _map = map;
            _player = player;
            _rng = rng;
        }

        /// <summary>Per-frame driver used by the game loop (input + animation).</summary>
        public void Update(GameTime gameTime)
        {
            float deltaMs = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            switch (Phase)
            {
                case TurnPhase.Animating:
                    bool anyAnimating = false;
                    foreach (Actor actor in _map.Actors)
                    {
                        actor.UpdateAnimation(deltaMs);
                        anyAnimating |= actor.IsAnimating;
                    }
                    if (!anyAnimating) Phase = TurnPhase.AwaitingInput;
                    break;

                case TurnPhase.AwaitingInput:
                    TurnAction action = _player.ReadInput(KeyboardManager.Instance, deltaMs);
                    if (action != null) ExecuteTurn(action);
                    break;
            }
        }

        /// <summary>
        /// Resolve one full turn from the player's chosen action. Returns false when
        /// the action was illegal (e.g. walking into a wall) and no turn was consumed.
        /// Pure game logic — no graphics or input dependencies.
        /// </summary>
        public bool ExecuteTurn(TurnAction playerAction)
        {
            // Tiles that will be occupied once this turn's moves finish.
            var reservedTiles = new HashSet<Point>();

            if (playerAction is MoveAction playerMove)
            {
                // Bumping a wall turns the player in place without consuming a turn.
                _player.Facing = playerMove.Direction;

                Point target = _player.GridPosition + playerMove.Direction.ToOffset();
                if (!_map.CanMove(_player.GridPosition, playerMove.Direction) || _map.IsOccupied(target))
                    return false;

                _player.BeginMove(target);
            }
            reservedTiles.Add(_player.GridPosition);

            ResolveEnemyTurns(reservedTiles);

            TurnCount++;
            Phase = TurnPhase.Animating;
            return true;
        }

        private void ResolveEnemyTurns(HashSet<Point> reservedTiles)
        {
            // Tiles of enemies that haven't decided yet: they might stay put,
            // so nobody may move into them. A tile just vacated by an earlier
            // mover is fair game (PMD-style follow chains).
            var undecidedTiles = new HashSet<Point>();
            var enemies = new List<Enemy>();
            foreach (Actor actor in _map.Actors)
            {
                if (actor is Enemy enemy)
                {
                    enemies.Add(enemy);
                    undecidedTiles.Add(enemy.GridPosition);
                }
            }

            foreach (Enemy enemy in enemies)
            {
                undecidedTiles.Remove(enemy.GridPosition);

                bool IsTileFree(Point p) => !reservedTiles.Contains(p) && !undecidedTiles.Contains(p);

                TurnAction action = enemy.DecideAction(_map, _player.GridPosition, IsTileFree, _rng);
                if (action is MoveAction move)
                {
                    Point target = enemy.GridPosition + move.Direction.ToOffset();
                    enemy.Facing = move.Direction;
                    enemy.BeginMove(target);
                }

                reservedTiles.Add(enemy.GridPosition);
            }
        }
    }
}
