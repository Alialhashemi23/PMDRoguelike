using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Combat;
using PMDRoguelike.Constants;
using PMDRoguelike.Core;
using PMDRoguelike.Data;
using PMDRoguelike.Managers;
using PMDRoguelike.Turns;
using PMDRoguelike.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMDRoguelike.Entities
{
    /// <summary>
    /// The player-controlled Pokémon: translates keyboard state into TurnActions
    /// and owns EXP/level progression.
    /// </summary>
    public class Player : Actor
    {
        private float _heldMs;

        public int Exp { get; private set; }

        /// <summary>RoR-style item collection (passive stacks + active slots).</summary>
        public Items.Inventory Inventory { get; }

        /// <summary>Moves earned by leveling while already knowing four — resolved via the learn prompt.</summary>
        public List<MoveDefinition> PendingMoveLearns { get; } = new List<MoveDefinition>();

        public Player(Point gridPosition, SpeciesDefinition species, int level)
            : base(gridPosition, species, level)
        {
            Inventory = new Items.Inventory(this);
        }

        /// <summary>Item stat modifiers layer on top of level-derived stats.</summary>
        protected override StatBlock ComputeStats()
        {
            StatBlock stats = base.ComputeStats();
            // Null during the base constructor (before Inventory is assigned).
            return Inventory == null ? stats : Inventory.ApplyStatModifiers(stats);
        }

        public int ExpToNextLevel => ExpRequired(Level);

        /// <summary>EXP needed to go from the given level to the next.</summary>
        public static int ExpRequired(int level)
        {
            var cfg = GameConstants.Instance.Data.GameMechanics.Experience;
            return Math.Max(1, (int)(cfg.BaseExpRequired * Math.Pow(level, cfg.ExpScaleFactor)) / 10);
        }

        /// <summary>
        /// Award EXP, applying any level-ups: stats recalc, HP rises by the gain, and
        /// learnset moves are learned (or queued for the replace prompt when full).
        /// </summary>
        public void AddExp(int amount, MessageLog log)
        {
            int maxLevel = GameConstants.Instance.Data.GameMechanics.Experience.MaxLevel;
            Exp += amount;

            while (Level < maxLevel && Exp >= ExpToNextLevel)
            {
                Exp -= ExpToNextLevel;
                Level++;
                RefreshStats();
                log.Add($"{DisplayName} grew to level {Level}!");

                foreach (LearnsetEntry entry in Species.Learnset.Where(e => e.Level == Level))
                {
                    MoveDefinition move = GameData.GetMove(entry.Move);
                    if (Moves.Count < MaxMoves)
                    {
                        Moves.Add(new MoveSlot(move));
                        log.Add($"{DisplayName} learned {move.Name}!");
                    }
                    else
                    {
                        PendingMoveLearns.Add(move);
                    }
                }
            }
        }

        /// <summary>
        /// Poll input for this frame and return an action once one is committed, or null.
        /// Movement uses a short grace delay so diagonals register cleanly; attacks
        /// (1-4) and waiting (Space) trigger on key press.
        /// </summary>
        public TurnAction ReadInput(KeyboardManager keyboard, float deltaMs)
        {
            if (keyboard.WasKeyJustPressed(Keys.Space))
            {
                _heldMs = 0f;
                return new WaitAction();
            }

            for (int i = 0; i < MaxMoves; i++)
            {
                if (keyboard.WasKeyJustPressed(Keys.D1 + i) || keyboard.WasKeyJustPressed(Keys.NumPad1 + i))
                {
                    _heldMs = 0f;
                    return new AttackAction(i);
                }
            }

            if (keyboard.WasKeyJustPressed(Keys.Q)) { _heldMs = 0f; return new UseItemAction(0); }
            if (keyboard.WasKeyJustPressed(Keys.E)) { _heldMs = 0f; return new UseItemAction(1); }

            Direction held = keyboard.GetHeldDirection();
            if (held == Direction.None)
            {
                _heldMs = 0f;
                return null;
            }

            _heldMs += deltaMs;
            if (_heldMs < GameConstants.Instance.InputDelayMs) return null;

            // Not resetting _heldMs means a held key keeps issuing moves turn after turn.
            return new MoveAction(held);
        }
    }
}
