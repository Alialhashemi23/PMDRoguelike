using PMDRoguelike.Combat;
using PMDRoguelike.Core;
using PMDRoguelike.Data;
using PMDRoguelike.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PMDRoguelike.Items
{
    /// <summary>
    /// Every concrete item plus the registry/roller. Items are stateless singletons;
    /// all numbers are per-stack unless noted.
    /// </summary>
    public static class ItemRegistry
    {
        private static readonly Dictionary<string, Item> _items;

        // Tier weights for random rolls (drops, floor spawns; chests/shops reuse in Phase 6).
        private const float CommonWeight = 0.55f;
        private const float UncommonWeight = 0.30f;
        private const float LegendaryWeight = 0.10f; // remainder (0.05) = Active

        static ItemRegistry()
        {
            _items = new Item[]
            {
                // Common (white)
                new OranBerry(), new SilkScarf(), new Leftovers(), new ShellBell(), new ScopeLens(),
                // Uncommon (green)
                new Eviolite(), new RockyHelmet(), new MuscleBand(), new LumCharm(),
                // Legendary (red)
                new ChoiceBand(), new LifeOrb(), new FocusSash(),
                // Active (orange)
                new EscapeRope(), new BlastSeed(), new LumBerry()
            }.ToDictionary(item => item.Id);
        }

        public static IReadOnlyCollection<Item> All => _items.Values;

        public static Item Get(string id) =>
            _items.TryGetValue(id, out Item item) ? item : throw new KeyNotFoundException($"Unknown item '{id}'");

        /// <summary>Roll a random item: tier by rarity weight, then uniform within the tier.</summary>
        public static Item Roll(Rng rng)
        {
            float roll = rng.NextFloat();
            ItemTier tier = roll < CommonWeight ? ItemTier.Common
                : roll < CommonWeight + UncommonWeight ? ItemTier.Uncommon
                : roll < CommonWeight + UncommonWeight + LegendaryWeight ? ItemTier.Legendary
                : ItemTier.Active;

            var pool = _items.Values.Where(item => item.Tier == tier).ToList();
            return pool[rng.Next(pool.Count)];
        }
    }

    // ====================================================================
    // Common (white)
    // ====================================================================

    public class OranBerry : PassiveItem
    {
        private const int HpPerStack = 10;

        public OranBerry() : base("oran-berry", "Oran Berry",
            $"+{HpPerStack} max HP per stack.", ItemTier.Common) { }

        public override void ModifyStats(ref StatBlock stats, int stacks) => stats.HP += HpPerStack * stacks;
    }

    public class SilkScarf : PassiveItem
    {
        private const float BonusPerStack = 0.15f;

        public SilkScarf() : base("silk-scarf", "Silk Scarf",
            "+15% Normal-type damage per stack.", ItemTier.Common) { }

        public override float ModifyOutgoingDamage(float damage, MoveDefinition move, int stacks) =>
            move.Type == PokemonType.Normal ? damage * (1f + BonusPerStack * stacks) : damage;
    }

    public class Leftovers : PassiveItem
    {
        private const float HealFractionPerStack = 0.01f;

        public Leftovers() : base("leftovers", "Leftovers",
            "Heal 1% of max HP each turn per stack.", ItemTier.Common) { }

        public override void OnTurnEnd(ItemContext context, int stacks)
        {
            Player player = context.Player;
            if (player.CurrentHP >= player.Stats.HP || player.IsFainted) return;
            player.Heal(HealAmount(player.Stats.HP, stacks));
        }

        public static int HealAmount(int maxHP, int stacks) =>
            Math.Max(1, (int)(maxHP * HealFractionPerStack * stacks));
    }

    public class ShellBell : PassiveItem
    {
        private const float HealFractionPerStack = 0.05f;

        public ShellBell() : base("shell-bell", "Shell Bell",
            "Heal 5% of damage you deal per stack.", ItemTier.Common) { }

        public override void OnDealtDamage(ItemContext context, Actor target, int damage, MoveDefinition move, int stacks)
        {
            if (damage <= 0) return;
            context.Player.Heal(Math.Max(1, (int)(damage * HealFractionPerStack * stacks)));
        }
    }

    public class ScopeLens : PassiveItem
    {
        private const float BonusPerStack = 0.05f;
        public const float HardCap = 0.50f;

        public ScopeLens() : base("scope-lens", "Scope Lens",
            "+5% critical-hit chance per stack (max +50%).", ItemTier.Common) { }

        public override float CritChanceBonus(int stacks) => Math.Min(HardCap, BonusPerStack * stacks);
    }

    // ====================================================================
    // Uncommon (green)
    // ====================================================================

    public class Eviolite : PassiveItem
    {
        private const float BonusPerStack = 0.10f;

        public Eviolite() : base("eviolite", "Eviolite",
            "+10% Defense and Sp. Defense per stack.", ItemTier.Uncommon) { }

        public override void ModifyStats(ref StatBlock stats, int stacks)
        {
            float factor = 1f + BonusPerStack * stacks;
            stats.Defense = (int)(stats.Defense * factor);
            stats.SpDefense = (int)(stats.SpDefense * factor);
        }
    }

    public class RockyHelmet : PassiveItem
    {
        public const int DamagePerStack = 3;

        public RockyHelmet() : base("rocky-helmet", "Rocky Helmet",
            $"Attackers that hit you in melee take {DamagePerStack} damage per stack.", ItemTier.Uncommon) { }

        public override void OnHolderHitByMelee(ItemContext context, Actor attacker, int stacks)
        {
            context.Log.Add($"{attacker.DisplayName} is hurt by the Rocky Helmet!");
            context.Combat.ApplyDirectDamage(context.Player, attacker, DamagePerStack * stacks);
        }
    }

    public class MuscleBand : PassiveItem
    {
        private const float BonusPerStack = 0.08f;

        public MuscleBand() : base("muscle-band", "Muscle Band",
            "+8% physical damage per stack.", ItemTier.Uncommon) { }

        public override float ModifyOutgoingDamage(float damage, MoveDefinition move, int stacks) =>
            move.Category == MoveCategory.Physical ? damage * (1f + BonusPerStack * stacks) : damage;
    }

    public class LumCharm : PassiveItem
    {
        private const float ChancePerStack = 0.15f;

        public LumCharm() : base("lum-charm", "Lum Charm",
            "15% chance per stack each turn to cure your status.", ItemTier.Uncommon) { }

        public override void OnTurnEnd(ItemContext context, int stacks)
        {
            Player player = context.Player;
            if (player.StatusType == StatusType.None) return;
            if (context.Rng.Chance(Math.Min(1f, ChancePerStack * stacks)))
            {
                context.Log.Add($"The Lum Charm cured {player.DisplayName}'s {player.StatusType.ToString().ToLower()}!");
                player.CureStatus();
            }
        }
    }

    // ====================================================================
    // Legendary (red)
    // ====================================================================

    public class ChoiceBand : PassiveItem
    {
        private const float BonusPerStack = 0.50f;

        public ChoiceBand() : base("choice-band", "Choice Band",
            "+50% damage per stack, but locks your first move until the next floor.", ItemTier.Legendary) { }

        public override float ModifyOutgoingDamage(float damage, MoveDefinition move, int stacks) =>
            damage * (1f + BonusPerStack * stacks);

        public override void OnMoveUsed(ItemContext context, MoveDefinition move, int stacks)
        {
            if (context.Inventory.LockedMoveId != null) return;
            context.Inventory.LockedMoveId = move.Id;
            context.Log.Add($"The Choice Band locks you into {move.Name} until the next floor!");
        }
    }

    public class LifeOrb : PassiveItem
    {
        private const float BonusPerStack = 0.30f;
        private const float RecoilFraction = 0.08f; // flat, does not stack

        public LifeOrb() : base("life-orb", "Life Orb",
            "+30% damage per stack; lose 8% max HP when you deal damage.", ItemTier.Legendary) { }

        public override float ModifyOutgoingDamage(float damage, MoveDefinition move, int stacks) =>
            damage * (1f + BonusPerStack * stacks);

        public override void OnDealtDamage(ItemContext context, Actor target, int damage, MoveDefinition move, int stacks)
        {
            if (damage <= 0) return;
            Player player = context.Player;
            int recoil = Math.Max(1, (int)(player.Stats.HP * RecoilFraction));
            player.TakeDamage(recoil);
            context.Log.Add($"{player.DisplayName} is hurt by the Life Orb!");
            if (player.IsFainted) context.Combat.NotifyFaint(player, player);
        }
    }

    public class FocusSash : PassiveItem
    {
        public FocusSash() : base("focus-sash", "Focus Sash",
            "Survive a lethal hit at 1 HP, once per floor per stack.", ItemTier.Legendary) { }

        public override int ModifyLethalDamage(int damage, int currentHP, ItemContext context, int stacks)
        {
            if (damage < currentHP) return damage;
            if (context.Inventory.SashChargesUsed >= stacks) return damage;

            context.Inventory.SashChargesUsed++;
            context.Log.Add($"{context.Player.DisplayName} hung on with the Focus Sash!");
            return currentHP - 1;
        }
    }

    // ====================================================================
    // Active (orange)
    // ====================================================================

    public class EscapeRope : ActiveItem
    {
        public EscapeRope() : base("escape-rope", "Escape Rope",
            "Warp straight to this floor's stairs.") { }

        public override bool Activate(ItemContext context)
        {
            if (context.Map.IsOccupied(context.Map.StairsPosition))
            {
                context.Log.Add("Something is standing on the stairs!");
                return false;
            }

            context.Player.SnapTo(context.Map.StairsPosition);
            context.Log.Add("The Escape Rope whisks you to the stairs!");
            return true;
        }
    }

    public class BlastSeed : ActiveItem
    {
        public const int Damage = 40;

        private static readonly MoveDefinition ThrowProfile = new MoveDefinition
        {
            Id = "blast-seed-throw",
            Name = "Blast Seed",
            Range = MoveRange.Line,
            Distance = 2
        };

        public BlastSeed() : base("blast-seed", "Blast Seed",
            $"Throw for {Damage} flat damage (2-tile line).") { }

        public override bool Activate(ItemContext context)
        {
            Actor target = Targeting.FindTarget(context.Map, context.Player, ThrowProfile);
            if (target == null)
            {
                context.Log.Add("No target in range for the Blast Seed.");
                return false;
            }

            context.Log.Add($"The Blast Seed explodes on {target.DisplayName}!");
            context.Combat.ApplyDirectDamage(context.Player, target, Damage);
            return true;
        }
    }

    public class LumBerry : ActiveItem
    {
        private const int HealAmount = 25;

        public LumBerry() : base("lum-berry", "Lum Berry",
            $"Cure your status and heal {HealAmount} HP.") { }

        public override bool Activate(ItemContext context)
        {
            Player player = context.Player;
            if (player.StatusType == StatusType.None && player.CurrentHP >= player.Stats.HP)
            {
                context.Log.Add("You're already in top shape.");
                return false;
            }

            if (player.StatusType != StatusType.None)
            {
                context.Log.Add($"The Lum Berry cured {player.DisplayName}'s {player.StatusType.ToString().ToLower()}!");
                player.CureStatus();
            }
            player.Heal(HealAmount);
            context.Log.Add($"{player.DisplayName} ate the Lum Berry!");
            return true;
        }
    }
}
