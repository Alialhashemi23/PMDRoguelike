using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PMDRoguelike.Constants;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;

namespace PMDRoguelike.Rendering
{
    /// <summary>
    /// Draws the dungeon and its entities. Everything is looked up by logical
    /// sprite key through GameContentManager, currently resolving to tinted
    /// placeholder squares — swapping in real PMD-style sprite sheets later only
    /// requires changing what those keys resolve to.
    /// </summary>
    public class DungeonRenderer
    {
        private readonly GameContentManager _content;

        public DungeonRenderer(GameContentManager content)
        {
            _content = content;

            // Placeholder palette. Replace with real textures by registering
            // loaded sprites under the same keys. Tile colors are overridden
            // per-dungeon; species colors are registered when actors spawn.
            _content.RegisterSolid("tile.wall", new Color(58, 58, 72));
            _content.RegisterSolid("tile.floor", new Color(198, 189, 165));
            _content.RegisterSolid("tile.stairs", new Color(96, 134, 222));
            _content.RegisterSolid("tile.shopkeeper", new Color(64, 170, 158));
            _content.RegisterSolid("entity.unknown", Color.Magenta);
            _content.RegisterSolid("ui.pixel", Color.White);
        }

        public void Draw(SpriteBatch spriteBatch, DungeonMap map)
        {
            int tileSize = GameConstants.Instance.TileSize;

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    Tile tile = map.GetTile(new Point(x, y));
                    Texture2D texture = _content.GetTexture(tile.SpriteKey);
                    var destination = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                    spriteBatch.Draw(texture, destination, Color.White);
                }
            }

            Texture2D pixel = _content.GetTexture("ui.pixel");

            // Ground items: tier-colored pips sitting on tiles.
            foreach (Items.GroundItem ground in map.GroundItems)
            {
                DrawPip(spriteBatch, pixel, ground.Position, tileSize, Items.Item.TierColor(ground.Item.Tier), 10);
            }

            // Money piles: small gold pips.
            foreach (Items.MoneyPile pile in map.MoneyPiles)
            {
                DrawPip(spriteBatch, pixel, pile.Position, tileSize, new Color(235, 200, 90), 8);
            }

            // Chests: brown boxes with a gold clasp.
            foreach (Items.Chest chest in map.Chests)
            {
                int px = chest.Position.X * tileSize;
                int py = chest.Position.Y * tileSize;
                spriteBatch.Draw(pixel, new Rectangle(px + 6, py + 8, tileSize - 12, tileSize - 14), new Color(50, 34, 20));
                spriteBatch.Draw(pixel, new Rectangle(px + 8, py + 10, tileSize - 16, tileSize - 18), new Color(122, 82, 44));
                spriteBatch.Draw(pixel, new Rectangle(px + tileSize / 2 - 2, py + 12, 4, 8), new Color(235, 200, 90));
            }

            // Shop stock: tier pips with a white "for sale" ring.
            foreach (Items.ShopItem shopItem in map.ShopItems)
            {
                int px = shopItem.Position.X * tileSize;
                int py = shopItem.Position.Y * tileSize;
                spriteBatch.Draw(pixel, new Rectangle(px + tileSize / 2 - 9, py + tileSize / 2 - 9, 18, 18), Color.White);
                DrawPip(spriteBatch, pixel, shopItem.Position, tileSize, Items.Item.TierColor(shopItem.Item.Tier), 12);
            }

            foreach (Actor actor in map.Actors)
            {
                Texture2D texture = _content.GetTexture(actor.SpriteKey);
                Vector2 drawPos = actor.RenderPosition + actor.VisualOffset;

                // The player gets a white outline; bosses get a gold one.
                if (actor is Player)
                {
                    spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X, (int)drawPos.Y, tileSize, tileSize),
                        Color.White * 0.9f);
                }
                else if (actor is Boss)
                {
                    spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X - 2, (int)drawPos.Y - 2, tileSize + 4, tileSize + 4),
                        new Color(235, 190, 80));
                }

                // Slight inset so actors read as pieces standing on tiles.
                var destination = new Rectangle(
                    (int)drawPos.X + 2,
                    (int)drawPos.Y + 2,
                    tileSize - 4,
                    tileSize - 4);
                spriteBatch.Draw(texture, destination, Color.White);

                // Hit flash overlay.
                if (actor.HitFlash > 0f)
                {
                    spriteBatch.Draw(pixel, destination, Color.White * (actor.HitFlash * 0.8f));
                }

                // Status indicator: small colored square in the tile corner.
                if (actor.StatusType != Combat.StatusType.None)
                {
                    var dot = new Rectangle((int)drawPos.X + tileSize - 9, (int)drawPos.Y + 1, 8, 8);
                    spriteBatch.Draw(pixel, dot, HudRenderer.StatusColor(actor.StatusType));
                }
            }
        }

        private static void DrawPip(SpriteBatch spriteBatch, Texture2D pixel, Point tile, int tileSize,
            Color color, int size)
        {
            int px = tile.X * tileSize + tileSize / 2;
            int py = tile.Y * tileSize + tileSize / 2;
            spriteBatch.Draw(pixel, new Rectangle(px - size / 2 - 2, py - size / 2 - 2, size + 4, size + 4),
                Color.Black * 0.6f);
            spriteBatch.Draw(pixel, new Rectangle(px - size / 2, py - size / 2, size, size), color);
        }
    }
}
