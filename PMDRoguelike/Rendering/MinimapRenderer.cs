using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;

namespace PMDRoguelike.Rendering
{
    /// <summary>
    /// PMD-style corner minimap: explored layout, stairs, chests/shops, and any
    /// actors currently in sight. Unexplored tiles simply don't exist on it.
    /// </summary>
    public static class MinimapRenderer
    {
        private const int Scale = 3;

        public static void Draw(SpriteBatch spriteBatch, Texture2D pixel, DungeonMap map, int viewportWidth)
        {
            int w = map.Width * Scale;
            int h = map.Height * Scale;
            int x0 = viewportWidth - w - 12;
            int y0 = 92;

            spriteBatch.Draw(pixel, new Rectangle(x0 - 4, y0 - 4, w + 8, h + 8), Color.Black * 0.55f);

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var p = new Point(x, y);
                    if (!map.IsExplored(p)) continue;

                    Tile tile = map.GetTile(p);
                    Color color = tile.Type switch
                    {
                        TileType.Wall => new Color(70, 70, 84),
                        TileType.Stairs => new Color(120, 160, 255),
                        TileType.Shopkeeper => new Color(64, 170, 158),
                        _ => new Color(150, 145, 130)
                    };
                    spriteBatch.Draw(pixel, new Rectangle(x0 + x * Scale, y0 + y * Scale, Scale, Scale),
                        color * (map.IsVisible(p) ? 0.95f : 0.6f));
                }
            }

            foreach (Items.Chest chest in map.Chests)
            {
                if (!map.IsExplored(chest.Position)) continue;
                DrawDot(spriteBatch, pixel, x0, y0, chest.Position, new Color(235, 200, 90));
            }
            foreach (Items.ShopItem stock in map.ShopItems)
            {
                if (!map.IsExplored(stock.Position)) continue;
                DrawDot(spriteBatch, pixel, x0, y0, stock.Position, Color.White);
            }

            foreach (Actor actor in map.Actors)
            {
                if (!map.IsVisible(actor.GridPosition)) continue;
                Color color = actor switch
                {
                    Player => Color.White,
                    Boss => new Color(235, 190, 80),
                    _ => new Color(220, 80, 80)
                };
                DrawDot(spriteBatch, pixel, x0, y0, actor.GridPosition, color, expand: actor is Player ? 1 : 0);
            }
        }

        private static void DrawDot(SpriteBatch spriteBatch, Texture2D pixel, int x0, int y0, Point p,
            Color color, int expand = 0)
        {
            spriteBatch.Draw(pixel, new Rectangle(
                x0 + p.X * Scale - expand, y0 + p.Y * Scale - expand,
                Scale + expand * 2, Scale + expand * 2), color);
        }
    }
}
