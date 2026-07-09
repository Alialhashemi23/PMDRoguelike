using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Entities;
using System;

namespace PMDRoguelike.Combat
{
    public enum StatusType
    {
        None,
        Burn,
        Poison,
        Paralysis,
        Sleep
    }

    /// <summary>An active major status condition on an actor (one at a time, mainline-style).</summary>
    public class StatusEffect
    {
        public StatusType Type { get; }
        public int TurnsRemaining { get; set; }

        public StatusEffect(StatusType type, int turns)
        {
            Type = type;
            TurnsRemaining = turns;
        }
    }

    /// <summary>
    /// The rules for each status condition: durations (from GameConstants), per-turn
    /// damage, action skips, and log text. Kept static and graphics-free so both the
    /// TurnController and the headless tests use identical behavior.
    /// </summary>
    public static class StatusRules
    {
        /// <summary>Chance a paralyzed actor loses its action on a given turn.</summary>
        public const float ParalysisSkipChance = 0.25f;

        public static int DurationFor(StatusType type)
        {
            var cfg = GameConstants.Instance.Data.Combat.StatusEffects;
            return type switch
            {
                StatusType.Burn => cfg.BurnDuration,
                StatusType.Poison => cfg.PoisonDuration,
                StatusType.Paralysis => cfg.ParalyzeDuration,
                StatusType.Sleep => cfg.SleepDuration,
                _ => 0
            };
        }

        /// <summary>Damage this status deals to its carrier at the end of each turn (0 = none).</summary>
        public static int TickDamage(Actor actor) => actor.StatusType switch
        {
            StatusType.Burn => Math.Max(1, actor.Stats.HP / 16),
            StatusType.Poison => Math.Max(1, actor.Stats.HP / 8),
            _ => 0
        };

        /// <summary>
        /// Roll whether the actor loses its action this turn (sleep always, paralysis
        /// sometimes). Outputs the log line to show when the action is lost.
        /// </summary>
        public static bool ActionSkipped(Actor actor, Rng rng, out string message)
        {
            switch (actor.StatusType)
            {
                case StatusType.Sleep:
                    message = $"{actor.DisplayName} is fast asleep...";
                    return true;
                case StatusType.Paralysis when rng.Chance(ParalysisSkipChance):
                    message = $"{actor.DisplayName} is paralyzed! It can't move!";
                    return true;
                default:
                    message = null;
                    return false;
            }
        }

        public static string InflictMessage(Actor target, StatusType type) => type switch
        {
            StatusType.Burn => $"{target.DisplayName} was burned!",
            StatusType.Poison => $"{target.DisplayName} was poisoned!",
            StatusType.Paralysis => $"{target.DisplayName} is paralyzed! It may be unable to move!",
            StatusType.Sleep => $"{target.DisplayName} fell asleep!",
            _ => null
        };

        public static string WearOffMessage(Actor target, StatusType type) => type switch
        {
            StatusType.Burn => $"{target.DisplayName}'s burn healed.",
            StatusType.Poison => $"{target.DisplayName}'s poison wore off.",
            StatusType.Paralysis => $"{target.DisplayName} is no longer paralyzed.",
            StatusType.Sleep => $"{target.DisplayName} woke up!",
            _ => null
        };

        /// <summary>Short HUD label, e.g. "PSN".</summary>
        public static string Abbreviation(StatusType type) => type switch
        {
            StatusType.Burn => "BRN",
            StatusType.Poison => "PSN",
            StatusType.Paralysis => "PAR",
            StatusType.Sleep => "SLP",
            _ => ""
        };
    }
}
