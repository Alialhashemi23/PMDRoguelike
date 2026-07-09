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

            if (isStruggle && !attacker.IsFainted)
            {
                int recoil = Math.Max(1, result.Damage / 4);
                attacker.TakeDamage(recoil);
                _log.Add($"{attacker.DisplayName} is hit with recoil!");
                if (attacker.IsFainted) HandleFaint(attacker, attacker);
            }
        }

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
