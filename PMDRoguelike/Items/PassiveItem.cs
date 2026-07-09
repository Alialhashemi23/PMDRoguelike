using PMDRoguelike.Data;
using PMDRoguelike.Entities;

namespace PMDRoguelike.Items
{
    /// <summary>
    /// A stacking passive. Every hook receives the holder's stack count and must
    /// scale its effect by it (flat stacking, RoR-style). Hard/soft caps are the
    /// item's own responsibility (e.g. Scope Lens clamps its crit bonus).
    /// All hooks default to no-ops so items only override what they use.
    /// </summary>
    public abstract class PassiveItem : Item
    {
        protected PassiveItem(string id, string name, string description, ItemTier tier)
            : base(id, name, description, tier) { }

        /// <summary>Adjust the holder's computed stats (max HP, defenses, ...).</summary>
        public virtual void ModifyStats(ref StatBlock stats, int stacks) { }

        /// <summary>Scale damage the holder deals before it's applied.</summary>
        public virtual float ModifyOutgoingDamage(float damage, MoveDefinition move, int stacks) => damage;

        /// <summary>Additive crit chance bonus (0..1). Cap yourself.</summary>
        public virtual float CritChanceBonus(int stacks) => 0f;

        /// <summary>Fires at the end of every turn.</summary>
        public virtual void OnTurnEnd(ItemContext context, int stacks) { }

        /// <summary>Fires when a new floor begins (after per-floor inventory state resets).</summary>
        public virtual void OnFloorStart(ItemContext context, int stacks) { }

        /// <summary>Fires after the holder damages a target (target may already be fainted).</summary>
        public virtual void OnDealtDamage(ItemContext context, Actor target, int damage, MoveDefinition move, int stacks) { }

        /// <summary>Fires after the holder is hit by a melee move (attacker still alive).</summary>
        public virtual void OnHolderHitByMelee(ItemContext context, Actor attacker, int stacks) { }

        /// <summary>Fires when the holder uses any real move (not Struggle).</summary>
        public virtual void OnMoveUsed(ItemContext context, MoveDefinition move, int stacks) { }

        /// <summary>
        /// Last chance to reduce a hit that would faint the holder
        /// (damage &gt;= currentHP). Return the adjusted damage.
        /// </summary>
        public virtual int ModifyLethalDamage(int damage, int currentHP, ItemContext context, int stacks) => damage;
    }

    /// <summary>
    /// A manually triggered single-use item occupying one of the limited active
    /// slots. Activate returns false (and logs why) when it can't be used —
    /// the item is kept and no turn passes.
    /// </summary>
    public abstract class ActiveItem : Item
    {
        protected ActiveItem(string id, string name, string description)
            : base(id, name, description, ItemTier.Active) { }

        public abstract bool Activate(ItemContext context);
    }
}
