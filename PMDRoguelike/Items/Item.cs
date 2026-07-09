using Microsoft.Xna.Framework;

namespace PMDRoguelike.Items
{
    /// <summary>
    /// Rarity/behavior tiers from the Risk-of-Rain-style item system.
    /// Passive tiers (Common/Uncommon/Legendary) stack flatly in the passive
    /// inventory; Active items occupy limited slots and are triggered manually.
    /// </summary>
    public enum ItemTier
    {
        Common,     // White
        Uncommon,   // Green
        Legendary,  // Red
        Active      // Orange
    }

    /// <summary>
    /// Base class for all items. Concrete items live in ItemCatalog.cs; they are
    /// stateless singletons — per-run state (stack counts, Choice Band lock, Focus
    /// Sash charges) lives in the player's Inventory.
    /// </summary>
    public abstract class Item
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public ItemTier Tier { get; }

        /// <summary>Logical texture key resolved by the renderer.</summary>
        public string SpriteKey => $"item.{Id}";

        protected Item(string id, string name, string description, ItemTier tier)
        {
            Id = id;
            Name = name;
            Description = description;
            Tier = tier;
        }

        public static Color TierColor(ItemTier tier) => tier switch
        {
            ItemTier.Common => new Color(240, 240, 240),
            ItemTier.Uncommon => new Color(110, 210, 100),
            ItemTier.Legendary => new Color(226, 80, 80),
            ItemTier.Active => new Color(240, 160, 64),
            _ => Color.Magenta
        };
    }
}
