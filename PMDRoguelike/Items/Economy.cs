using Microsoft.Xna.Framework;
using PMDRoguelike.Core;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.UI;

namespace PMDRoguelike.Items
{
    /// <summary>A locked chest on the floor: pay Poké, get a depth-weighted item.</summary>
    public class Chest
    {
        public Point Position { get; }
        public int Price { get; }

        public Chest(Point position, int price)
        {
            Position = position;
            Price = price;
        }
    }

    /// <summary>An item on display in a shop room. Not obtainable without paying.</summary>
    public class ShopItem
    {
        public Point Position { get; }
        public Item Item { get; }
        public int Price { get; }

        public ShopItem(Point position, Item item, int price)
        {
            Position = position;
            Item = item;
            Price = price;
        }
    }

    /// <summary>Loose Poké lying on a tile, picked up by walking over it.</summary>
    public class MoneyPile
    {
        public Point Position { get; }
        public int Amount { get; }

        public MoneyPile(Point position, int amount)
        {
            Position = position;
            Amount = amount;
        }
    }

    /// <summary>
    /// Prices and purchase/open flows. Static and graphics-free so the headless
    /// tests exercise exactly what the game runs.
    /// </summary>
    public static class Economy
    {
        /// <summary>Chest price before jitter; grows with run depth.</summary>
        public static int BaseChestPrice(int depth) => 40 + 15 * depth;

        public static int ChestPrice(int depth, Rng rng) => Jitter(BaseChestPrice(depth), rng);

        /// <summary>Shop price before jitter; tier base plus depth growth.</summary>
        public static int BaseItemPrice(ItemTier tier, int depth)
        {
            int tierBase = tier switch
            {
                ItemTier.Common => 60,
                ItemTier.Uncommon => 120,
                ItemTier.Legendary => 250,
                _ => 100 // Active
            };
            return tierBase + 8 * depth;
        }

        public static int ItemPrice(ItemTier tier, int depth, Rng rng) => Jitter(BaseItemPrice(tier, depth), rng);

        /// <summary>Poké gained for defeating an enemy of the given level.</summary>
        public static int FaintReward(int enemyLevel, Rng rng) => 4 + 2 * enemyLevel + rng.Next(0, 6);

        /// <summary>Amount in a floor money pile at the given depth.</summary>
        public static int PileAmount(int depth, Rng rng) => 10 + 5 * depth + rng.Next(0, 11);

        private static int Jitter(int value, Rng rng) => (int)(value * (0.9f + rng.NextFloat() * 0.2f));

        /// <summary>Buy a displayed shop item. False (and a log line) when it can't happen.</summary>
        public static bool TryBuy(Player player, DungeonMap map, ShopItem shopItem, MessageLog log)
        {
            if (player.Poke < shopItem.Price)
            {
                log.Add($"Not enough Poké — the {shopItem.Item.Name} costs {shopItem.Price}.");
                Core.AudioCues.Post("denied");
                return false;
            }

            // Full active slots refuse the purchase (AddItem logs why) — no charge.
            if (!player.Inventory.AddItem(shopItem.Item, log)) return false;

            player.SpendPoke(shopItem.Price);
            map.ShopItems.Remove(shopItem);
            log.Add($"Bought the {shopItem.Item.Name} for {shopItem.Price} Poké.");
            Core.AudioCues.Post("buy");
            return true;
        }

        /// <summary>Open a chest: pay, roll a depth-weighted item, hand it over.</summary>
        public static bool TryOpenChest(Player player, DungeonMap map, Chest chest, MessageLog log, Rng rng, int depth)
        {
            if (player.Poke < chest.Price)
            {
                log.Add($"The chest is locked — {chest.Price} Poké to open.");
                Core.AudioCues.Post("denied");
                return false;
            }

            player.SpendPoke(chest.Price);
            map.Chests.Remove(chest);
            Core.AudioCues.Post("chest");

            Item item = ItemRegistry.Roll(rng, depth);
            log.Add($"The chest creaks open... it held a {item.Name}!");
            if (!player.Inventory.AddItem(item, log))
            {
                // Active slots full: leave it on the chest tile for later.
                map.GroundItems.Add(new GroundItem(chest.Position, item));
            }
            return true;
        }
    }
}
