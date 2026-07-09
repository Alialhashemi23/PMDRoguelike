using PMDRoguelike.Data;
using PMDRoguelike.Entities;
using PMDRoguelike.UI;
using System;
using System.Collections.Generic;

namespace PMDRoguelike.Items
{
    /// <summary>
    /// The player's items: unbounded passive stacks plus a small number of active
    /// slots. Also holds per-run item state (Choice Band lock, Focus Sash charges)
    /// and dispatches every hook across the passive collection so effects compose.
    /// </summary>
    public class Inventory
    {
        public const int MaxActiveSlots = 2;

        private readonly Player _owner;
        private readonly Dictionary<string, int> _stacks = new();
        private readonly List<PassiveItem> _passives = new(); // pickup order, distinct

        public List<ActiveItem> Actives { get; } = new();

        // --- Per-floor item state (reset by OnFloorStart) ---
        /// <summary>Move id the Choice Band has locked in, or null.</summary>
        public string LockedMoveId { get; set; }
        /// <summary>Focus Sash charges consumed on this floor.</summary>
        public int SashChargesUsed { get; set; }

        public IReadOnlyList<PassiveItem> Passives => _passives;

        public Inventory(Player owner)
        {
            _owner = owner;
        }

        public int StacksOf(string itemId) => _stacks.TryGetValue(itemId, out int n) ? n : 0;

        /// <summary>
        /// Add a picked-up item. Passives always succeed and re-derive stats;
        /// actives fail (item stays on the ground) when all slots are full.
        /// </summary>
        public bool AddItem(Item item, MessageLog log)
        {
            switch (item)
            {
                case PassiveItem passive:
                    if (!_stacks.ContainsKey(passive.Id)) _passives.Add(passive);
                    _stacks[passive.Id] = StacksOf(passive.Id) + 1;
                    _owner.RefreshStats();
                    log.Add($"Picked up {passive.Name} (x{StacksOf(passive.Id)})!");
                    return true;

                case ActiveItem active:
                    if (Actives.Count >= MaxActiveSlots)
                    {
                        log.Add($"No room for the {active.Name} (active slots full).");
                        return false;
                    }
                    Actives.Add(active);
                    log.Add($"Picked up {active.Name}! ({(Actives.Count == 1 ? "Q" : "E")} to use)");
                    return true;

                default:
                    return false;
            }
        }

        // --- Hook aggregation -------------------------------------------------

        public StatBlock ApplyStatModifiers(StatBlock stats)
        {
            foreach (PassiveItem item in _passives)
                item.ModifyStats(ref stats, StacksOf(item.Id));
            return stats;
        }

        public int ModifyOutgoingDamage(int damage, MoveDefinition move)
        {
            float value = damage;
            foreach (PassiveItem item in _passives)
                value = item.ModifyOutgoingDamage(value, move, StacksOf(item.Id));
            return Math.Max(0, (int)value);
        }

        public float CritChanceBonus()
        {
            float bonus = 0f;
            foreach (PassiveItem item in _passives)
                bonus += item.CritChanceBonus(StacksOf(item.Id));
            return bonus;
        }

        public void OnTurnEnd(ItemContext context)
        {
            foreach (PassiveItem item in _passives)
                item.OnTurnEnd(context, StacksOf(item.Id));
        }

        public void OnFloorStart(ItemContext context)
        {
            if (LockedMoveId != null) context.Log.Add("The Choice Band's grip loosens.");
            LockedMoveId = null;
            SashChargesUsed = 0;

            foreach (PassiveItem item in _passives)
                item.OnFloorStart(context, StacksOf(item.Id));
        }

        public void OnDealtDamage(ItemContext context, Actor target, int damage, MoveDefinition move)
        {
            foreach (PassiveItem item in _passives)
                item.OnDealtDamage(context, target, damage, move, StacksOf(item.Id));
        }

        public void OnHolderHitByMelee(ItemContext context, Actor attacker)
        {
            foreach (PassiveItem item in _passives)
                item.OnHolderHitByMelee(context, attacker, StacksOf(item.Id));
        }

        public void OnMoveUsed(ItemContext context, MoveDefinition move)
        {
            foreach (PassiveItem item in _passives)
                item.OnMoveUsed(context, move, StacksOf(item.Id));
        }

        /// <summary>Give items a last chance to blunt a hit that would faint the holder.</summary>
        public int ModifyLethalDamage(int damage, int currentHP, ItemContext context)
        {
            foreach (PassiveItem item in _passives)
            {
                if (damage < currentHP) break;
                damage = item.ModifyLethalDamage(damage, currentHP, context, StacksOf(item.Id));
            }
            return damage;
        }
    }
}
