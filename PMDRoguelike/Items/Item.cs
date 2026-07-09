namespace PMDRoguelike.Items
{
    /// <summary>
    /// Rarity/behavior tiers from the Risk-of-Rain-style item system.
    /// Passive tiers (Common/Uncommon/Legendary) stack flatly in the passive
    /// inventory; Active items occupy limited relic slots and are triggered manually.
    /// </summary>
    public enum ItemTier
    {
        Common,     // White
        Uncommon,   // Green
        Legendary,  // Red
        Active      // Orange / relic
    }

    /// <summary>
    /// Base class for all items. Phase 1 only establishes the shape of the system;
    /// concrete items, stacking math, and caps arrive in the item-system phase.
    /// </summary>
    public abstract class Item
    {
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public ItemTier Tier { get; protected set; }

        /// <summary>Logical texture key resolved by the renderer.</summary>
        public string SpriteKey { get; protected set; } = "item.unknown";

        protected Item(string name, string description, ItemTier tier)
        {
            Name = name;
            Description = description;
            Tier = tier;
        }
    }
}
