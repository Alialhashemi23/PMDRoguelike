using Microsoft.Xna.Framework;
using PMDRoguelike.Combat;
using PMDRoguelike.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMDRoguelike.Entities
{
    /// <summary>
    /// An entity that participates in turns and combat: a Pokémon with a species,
    /// level-derived stats, HP, and up to four moves.
    /// </summary>
    public abstract class Actor : Entity
    {
        public const int MaxMoves = 4;

        public SpeciesDefinition Species { get; }
        public int Level { get; protected set; }
        public StatBlock Stats { get; protected set; }
        public int CurrentHP { get; protected set; }
        public List<MoveSlot> Moves { get; } = new List<MoveSlot>();

        public bool IsFainted => CurrentHP <= 0;
        public string DisplayName => Species.Name;
        public bool AllMovesOutOfPP => Moves.All(slot => !slot.HasPP);

        /// <summary>Active major status condition, or null. One at a time (mainline-style).</summary>
        public StatusEffect Status { get; private set; }
        public StatusType StatusType => Status?.Type ?? StatusType.None;

        /// <summary>Apply a status; fails (returns false) if one is already active.</summary>
        public bool ApplyStatus(StatusType type, int turns)
        {
            if (type == StatusType.None || Status != null) return false;
            Status = new StatusEffect(type, turns);
            return true;
        }

        /// <summary>Remove the active status (used by expiry now; berries/items in Phase 5).</summary>
        public void CureStatus() => Status = null;

        protected Actor(Point gridPosition, SpeciesDefinition species, int level) : base(gridPosition)
        {
            Species = species;
            Level = Math.Max(1, level);
            Stats = StatBlock.AtLevel(species.BaseStats, Level);
            CurrentHP = Stats.HP;
            SpriteKey = $"species.{species.Id}";

            // Know the last four moves reachable at this level.
            foreach (LearnsetEntry entry in species.Learnset.Where(e => e.Level <= Level).TakeLast(MaxMoves))
            {
                Moves.Add(new MoveSlot(GameData.GetMove(entry.Move)));
            }
        }

        public void TakeDamage(int amount) => CurrentHP = Math.Max(0, CurrentHP - Math.Max(0, amount));

        public void Heal(int amount) => CurrentHP = Math.Min(Stats.HP, CurrentHP + Math.Max(0, amount));

        public void RestoreAllPP()
        {
            foreach (MoveSlot slot in Moves) slot.CurrentPP = slot.Move.PP;
        }

        /// <summary>Recompute stats for the current level; HP rises by the max-HP gain (PMD-style).</summary>
        protected void RecalculateStats()
        {
            int previousMax = Stats.HP;
            Stats = StatBlock.AtLevel(Species.BaseStats, Level);
            CurrentHP = Math.Min(Stats.HP, CurrentHP + Math.Max(0, Stats.HP - previousMax));
        }
    }
}
