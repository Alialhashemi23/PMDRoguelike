using PMDRoguelike.Core;
using PMDRoguelike.Data;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.UI;
using System;

namespace PMDRoguelike.Combat
{
    /// <summary>
    /// Executes attacks: PP, targeting, damage, faint handling, and EXP awards.
    /// Pure game logic — writes to the message log, never to the screen.
    /// </summary>
    public class CombatResolver
    {
        private readonly DungeonMap _map;
        private readonly MessageLog _log;
        private readonly Rng _rng;

        /// <summary>Set when the player faints; the state machine reads this to end the run.</summary>
        public bool PlayerFainted { get; private set; }

        public CombatResolver(DungeonMap map, MessageLog log, Rng rng)
        {
            _map = map;
            _log = log;
            _rng = rng;
        }

        /// <summary>
        /// Resolve one attack along the attacker's facing. Missing a target still
        /// consumes the turn and PP (PMD behavior — aim before you fire).
        /// </summary>
        public void ExecuteAttack(Actor attacker, MoveSlot slot)
        {
            MoveDefinition move = slot.Move;
            bool isStruggle = ReferenceEquals(move, GameData.Struggle);
            if (!isStruggle) slot.CurrentPP = Math.Max(0, slot.CurrentPP - 1);

            attacker.BeginLunge();
            _log.Add($"{attacker.DisplayName} used {move.Name}!");

            Actor target = Targeting.FindTarget(_map, attacker, move);
            if (target == null)
            {
                _log.Add("...but there was nothing there.");
                return;
            }

            // Pure status moves: accuracy roll, then apply — no damage math.
            if (move.Category == MoveCategory.Status)
            {
                if (move.Accuracy < 100 && _rng.Next(100) >= move.Accuracy)
                {
                    _log.Add($"{attacker.DisplayName}'s attack missed!");
                    return;
                }
                if (!TryInflictStatus(target, move.InflictStatus, 100))
                {
                    _log.Add("But it failed!");
                }
                return;
            }

            DamageResult result = DamageCalculator.Calculate(attacker, target, move, _rng);
            if (result.Missed)
            {
                _log.Add($"{attacker.DisplayName}'s attack missed!");
                return;
            }

            if (result.Effectiveness <= 0f)
            {
                _log.Add($"It doesn't affect {target.DisplayName}...");
                return;
            }

            target.TakeDamage(result.Damage);
            target.FlashHit();

            if (result.IsCritical) _log.Add("A critical hit!");
            if (result.Effectiveness >= 2f) _log.Add("It's super effective!");
            else if (result.Effectiveness < 1f) _log.Add("It's not very effective...");

            if (target.IsFainted)
            {
                HandleFaint(attacker, target);
            }
            else if (move.InflictStatus != StatusType.None)
            {
                TryInflictStatus(target, move.InflictStatus, move.InflictChance);
            }

            if (isStruggle && !attacker.IsFainted)
            {
                int recoil = Math.Max(1, result.Damage / 4);
                attacker.TakeDamage(recoil);
                _log.Add($"{attacker.DisplayName} is hit with recoil!");
                if (attacker.IsFainted) HandleFaint(attacker, attacker);
            }
        }

        /// <summary>
        /// Roll and apply a status. Returns false when the roll fails or the target
        /// already carries a status (one major condition at a time).
        /// </summary>
        public bool TryInflictStatus(Actor target, StatusType type, int chance)
        {
            if (type == StatusType.None) return false;
            if (chance < 100 && _rng.Next(100) >= chance) return false;
            if (!target.ApplyStatus(type, StatusRules.DurationFor(type))) return false;

            _log.Add(StatusRules.InflictMessage(target, type));
            return true;
        }

        /// <summary>
        /// End-of-turn status upkeep for every actor: damage-over-time, duration
        /// countdown, and expiry. Status damage can faint (player included).
        /// </summary>
        public void TickStatuses(Player player)
        {
            foreach (Actor actor in _map.Actors.ToArray())
            {
                if (actor.Status == null) continue;

                StatusType type = actor.StatusType;
                int damage = StatusRules.TickDamage(actor);
                if (damage > 0)
                {
                    actor.TakeDamage(damage);
                    actor.FlashHit();
                    if (actor == player || IsNearPlayer(actor, player))
                        _log.Add($"{actor.DisplayName} is hurt by its {(type == StatusType.Burn ? "burn" : "poison")}!");

                    if (actor.IsFainted)
                    {
                        // Status kills credit the player: they're the only opponent.
                        HandleFaint(player, actor);
                        continue;
                    }
                }

                actor.Status.TurnsRemaining--;
                if (actor.Status.TurnsRemaining <= 0)
                {
                    actor.CureStatus();
                    if (actor == player || IsNearPlayer(actor, player))
                        _log.Add(StatusRules.WearOffMessage(actor, type));
                }
            }
        }

        /// <summary>Only narrate things happening close enough for the player to see.</summary>
        public static bool IsNearPlayer(Actor actor, Player player) =>
            Math.Max(Math.Abs(actor.GridPosition.X - player.GridPosition.X),
                     Math.Abs(actor.GridPosition.Y - player.GridPosition.Y)) <= 8;

        private void HandleFaint(Actor attacker, Actor victim)
        {
            _log.Add($"{victim.DisplayName} fainted!");

            if (victim is Player)
            {
                PlayerFainted = true;
                return;
            }

            _map.Actors.Remove(victim);

            if (attacker is Player player && victim is Enemy enemy)
            {
                int exp = Math.Max(1, enemy.Species.ExpYield * enemy.Level / 7);
                _log.Add($"Gained {exp} EXP!");
                player.AddExp(exp, _log);
            }
        }
    }
}
