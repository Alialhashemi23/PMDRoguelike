using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PMDRoguelike.Entities;
using PMDRoguelike.Items;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;

namespace PMDRoguelike.UI
{
    /// <summary>
    /// Item overlay shown while Tab is held: passive stacks grouped with their tier
    /// icons and colors, plus the two active slots.
    /// </summary>
    public static class InventoryPanel
    {
        private const float Scale = 0.75f;

        public static void Draw(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel,
            GameContentManager content, Player player, int viewportWidth)
        {
            if (font == null) return;

            Inventory inventory = player.Inventory;
            float lineHeight = font.LineSpacing * Scale;
            int lines = System.Math.Max(1, inventory.Passives.Count) + Inventory.MaxActiveSlots + 2;
            var box = new Rectangle(viewportWidth - 372, 44, 360, (int)(lines * lineHeight + 14));
            spriteBatch.Draw(pixel, box, Color.Black * 0.7f);

            float y = box.Y + 6;
            TextRenderer.DrawShadowed(spriteBatch, font, "Items", new Vector2(box.X + 10, y), Color.LightGray, Scale);
            y += lineHeight;

            if (inventory.Passives.Count == 0)
            {
                TextRenderer.DrawShadowed(spriteBatch, font, "No passive items yet.",
                    new Vector2(box.X + 10, y), Color.DimGray, Scale);
                y += lineHeight;
            }
            else
            {
                foreach (PassiveItem item in inventory.Passives)
                {
                    Texture2D icon = content.GetTexture($"Icons/{item.Tier.ToString().ToLowerInvariant()}");
                    spriteBatch.Draw(icon, new Rectangle(box.X + 8, (int)y + 2, 14, 14), Color.White);
                    TextRenderer.DrawShadowed(spriteBatch, font,
                        $"{item.Name} x{inventory.StacksOf(item.Id)}",
                        new Vector2(box.X + 28, y), Item.TierColor(item.Tier), Scale);
                    y += lineHeight;
                }
            }

            y += lineHeight * 0.3f;
            for (int i = 0; i < Inventory.MaxActiveSlots; i++)
            {
                string key = i == 0 ? "Q" : "E";
                string label = i < inventory.Actives.Count
                    ? $"[{key}] {inventory.Actives[i].Name}"
                    : $"[{key}] —";
                Color color = i < inventory.Actives.Count ? Item.TierColor(ItemTier.Active) : Color.DimGray;
                TextRenderer.DrawShadowed(spriteBatch, font, label, new Vector2(box.X + 10, y), color, Scale);
                y += lineHeight;
            }
        }
    }
}
