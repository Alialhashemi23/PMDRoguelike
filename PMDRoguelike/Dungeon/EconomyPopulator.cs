using Microsoft.Xna.Framework;
using PMDRoguelike.Core;
using PMDRoguelike.Items;

namespace PMDRoguelike.Dungeon
{
    /// <summary>
    /// Dresses a generated floor with the economy: 0-2 chests, 1-3 money piles, and
    /// (sometimes) a shop room — a shopkeeper tile with priced items on display.
    /// Static and graphics-free so generation is testable over many seeds.
    /// </summary>
    public static class EconomyPopulator
    {
        public const float ShopChance = 0.35f;
        public const int MaxChestsPerFloor = 2;
        public const int ShopStock = 3;

        public static void Populate(DungeonMap map, Point playerSpawn, Rng rng, int depth)
        {
            PlaceChests(map, playerSpawn, rng, depth);
            PlaceMoneyPiles(map, playerSpawn, rng, depth);
            if (rng.Chance(ShopChance)) TryPlaceShop(map, playerSpawn, rng, depth);
        }

        private static void PlaceChests(DungeonMap map, Point playerSpawn, Rng rng, int depth)
        {
            int count = rng.Next(0, MaxChestsPerFloor + 1);
            int attempts = count * 20;
            while (count > 0 && attempts-- > 0)
            {
                Point p = RandomRoomTile(map, rng);
                if (!IsPlaceable(map, p, playerSpawn)) continue;
                map.Chests.Add(new Chest(p, Economy.ChestPrice(depth, rng)));
                count--;
            }
        }

        private static void PlaceMoneyPiles(DungeonMap map, Point playerSpawn, Rng rng, int depth)
        {
            int count = rng.Next(1, 4);
            int attempts = count * 20;
            while (count > 0 && attempts-- > 0)
            {
                Point p = RandomRoomTile(map, rng);
                if (!IsPlaceable(map, p, playerSpawn)) continue;
                map.MoneyPiles.Add(new MoneyPile(p, Economy.PileAmount(depth, rng)));
                count--;
            }
        }

        private static void TryPlaceShop(DungeonMap map, Point playerSpawn, Rng rng, int depth)
        {
            // Pick a roomy room that holds neither the spawn nor the stairs.
            var candidates = new System.Collections.Generic.List<Rectangle>();
            foreach (Rectangle room in map.Rooms)
            {
                if (room.Width < 5 || room.Height < 5) continue;
                if (room.Contains(playerSpawn) || room.Contains(map.StairsPosition)) continue;
                candidates.Add(room);
            }
            if (candidates.Count == 0) return;

            Rectangle shopRoom = candidates[rng.Next(candidates.Count)];
            Point keeper = shopRoom.Center;
            if (!IsPlaceable(map, keeper, playerSpawn) || map.IsOccupied(keeper)) return;

            // Display items on the tiles beside the keeper.
            Point[] displayOffsets = { new(-1, 0), new(1, 0), new(0, 1), new(0, -1) };
            int placed = 0;
            foreach (Point offset in displayOffsets)
            {
                if (placed >= ShopStock) break;
                Point p = keeper + offset;
                if (!IsPlaceable(map, p, playerSpawn) || map.IsOccupied(p)) continue;

                Item item = ItemRegistry.Roll(rng, depth);
                map.ShopItems.Add(new ShopItem(p, item, Economy.ItemPrice(item.Tier, depth, rng)));
                placed++;
            }

            // Only seat the shopkeeper if there's actually stock.
            if (placed > 0) map.SetTile(keeper, TileType.Shopkeeper);
            else map.ShopItems.RemoveAll(s => shopRoom.Contains(s.Position));
        }

        private static Point RandomRoomTile(DungeonMap map, Rng rng)
        {
            Rectangle room = map.Rooms[rng.Next(map.Rooms.Count)];
            return new Point(rng.Next(room.Left, room.Right), rng.Next(room.Top, room.Bottom));
        }

        private static bool IsPlaceable(DungeonMap map, Point p, Point playerSpawn) =>
            map.IsWalkable(p) && p != playerSpawn && map.IsFeatureFree(p) && !map.IsOccupied(p);
    }
}
