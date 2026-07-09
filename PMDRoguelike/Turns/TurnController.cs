using Microsoft.Xna.Framework;
using PMDRoguelike.Combat;
using PMDRoguelike.Core;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;
using PMDRoguelike.UI;
using System.Collections.Generic;

namespace PMDRoguelike.Turns
{
    public enum TurnPhase
    {
        /// <summary>Waiting for the player to commit an action.</summary>
        AwaitingInput,
        /// <summary>Slide/attack animations from the last turn are playing out.</summary>
        Animating
    }

    /// <summary>
    /// Drives the turn loop: the player commits an action, every enemy then decides
    /// theirs, all resulting animations play in parallel, and control returns to the
    /// player. Turn resolution itself (ExecuteTurn) is graphics-free so it can run
    /// headlessly in tests.
    /// </summary>
    public class TurnController
    {
        private const int RegenIntervalTurns = 4;

        private readonly DungeonMap _map;
        private readonly Player _player;
        private readonly Rng _rng;
        private readonly MessageLog _log;
        private readonly CombatResolver _combat;

        public TurnPhase Phase { get; private set; } = TurnPhase.AwaitingInput;
        public int TurnCount { get; private set; }

        /// <summary>True once the player has fainted; the run is over.</summary>
        public bool PlayerDefeated => _combat.PlayerFainted;

        public TurnController(DungeonMap map, Player player, Rng rng, MessageLog log)
        {
            _map = map;
            _player = player;
            _rng = rng;
            _log = log;
            _combat = new CombatResolver(map, log, rng);
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
                    if (PlayerDefeated) return;
                    TurnAction action = _player.ReadInput(KeyboardManager.Instance, deltaMs);
                    if (action != null) ExecuteTurn(action);
                    break;
            }
        }

        /// <summary>
        /// Resolve one full turn from the player's chosen action. Returns false when
        /// the action was illegal (walking into a wall, using an empty move) and no
        /// turn was consumed. Pure game logic — no graphics or input dependencies.
        /// </summary>
        public bool ExecuteTurn(TurnAction playerAction)
        {
            if (PlayerDefeated) return false;

            // Sleep/paralysis can steal the player's action; the turn still passes.
            if (playerAction is not WaitAction && StatusRules.ActionSkipped(_player, _rng, out string skipMessage))
            {
                _log.Add(skipMessage);
                playerAction = new WaitAction();
            }

            switch (playerAction)
            {
                case MoveAction move:
                {
                    // Bumping a wall turns the player in place without consuming a turn.
                    _player.Facing = move.Direction;

                    Point target = _player.GridPosition + move.Direction.ToOffset();
                    if (!_map.CanMove(_player.GridPosition, move.Direction) || _map.IsOccupied(target))
                        return false;

                    _player.BeginMove(target);
                    break;
                }
                case AttackAction attack:
                {
                    MoveSlot slot = ResolvePlayerMoveSlot(attack.MoveIndex);
                    if (slot == null) return false;
                    _combat.ExecuteAttack(_player, slot);
                    break;
                }
                // WaitAction: nothing to do, the turn simply passes.
            }

            if (!PlayerDefeated) ResolveEnemyTurns();

            // End-of-turn status upkeep (burn/poison damage, durations).
            if (!PlayerDefeated) _combat.TickStatuses(_player);

            TurnCount++;

            // PMD-style slow regen from walking around.
            if (!PlayerDefeated && TurnCount % RegenIntervalTurns == 0) _player.Heal(1);

            Phase = TurnPhase.Animating;
            return true;
        }

        /// <summary>
        /// Validate an attack request: falls back to Struggle when everything is out
        /// of PP, refuses (with a log line) when just the chosen move is empty.
        /// </summary>
        private MoveSlot ResolvePlayerMoveSlot(int index)
        {
            if (_player.Moves.Count == 0 || _player.AllMovesOutOfPP)
            {
                _log.Add($"{_player.DisplayName} has no PP left...");
                return new MoveSlot(Data.GameData.Struggle);
            }

            if (index < 0 || index >= _player.Moves.Count) return null;

            MoveSlot slot = _player.Moves[index];
            if (!slot.HasPP)
            {
                _log.Add($"{slot.Move.Name} is out of PP!");
                return null;
            }

            return slot;
        }

        private void ResolveEnemyTurns()
        {
            // Tiles that will be occupied once this turn's moves finish.
            var reservedTiles = new HashSet<Point> { _player.GridPosition };

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
                if (PlayerDefeated) break;

                undecidedTiles.Remove(enemy.GridPosition);

                // Sleeping/paralyzed enemies can lose their action entirely.
                if (StatusRules.ActionSkipped(enemy, _rng, out string enemySkip))
                {
                    if (CombatResolver.IsNearPlayer(enemy, _player)) _log.Add(enemySkip);
                    reservedTiles.Add(enemy.GridPosition);
                    continue;
                }

                bool IsTileFree(Point p) => !reservedTiles.Contains(p) && !undecidedTiles.Contains(p);

                TurnAction action = enemy.DecideAction(_map, _player, IsTileFree, _rng);
                switch (action)
                {
                    case MoveAction move:
                    {
                        Point target = enemy.GridPosition + move.Direction.ToOffset();
                        enemy.Facing = move.Direction;
                        enemy.BeginMove(target);
                        break;
                    }
                    case AttackAction attack:
                        _combat.ExecuteAttack(enemy, enemy.Moves[attack.MoveIndex]);
                        break;
                }

                reservedTiles.Add(enemy.GridPosition);
            }
        }
    }
}
