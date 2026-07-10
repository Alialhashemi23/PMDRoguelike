using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PMDRoguelike.Constants;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;

namespace PMDRoguelike.Rendering
{
    /// <summary>
    /// Draws the dungeon and its entities under the fog of war using the sprite
    /// pipeline: per-dungeon tilesets (with wall-face variants where a wall meets
    /// open floor), prop sprites, tier icons, and per-species directional walk
    /// sheets. All textures resolve through GameContentManager by name, so real
    /// art replaces the generated placeholders file-by-file.
    /// </summary>
    public class DungeonRenderer
    {
        private const float DimFactor = 0.45f;

        private readonly GameContentManager _content;

        public DungeonRenderer(GameContentManager content)
        {
            _content = content;

            _content.RegisterSolid("entity.unknown", Color.Magenta);
            _content.RegisterSolid("ui.pixel", Color.White);
        }

        public void Draw(SpriteBatch spriteBatch, DungeonMap map, double totalMs)
        {
            int tileSize = GameConstants.Instance.TileSize;
            Texture2D pixel = _content.GetTexture("ui.pixel");
            Texture2D stairsTex = _content.GetTexture("Props/stairs");
            Texture2D keeperTex = _content.GetTexture("Props/shopkeeper");
            Texture2D floorTex = _content.GetTexture("tile.floor");
            Texture2D wallTex = _content.GetTexture("tile.wall");
            Texture2D wallFaceTex = _content.GetTexture("tile.wall_face");

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var p = new Point(x, y);
                    if (!map.IsExplored(p)) continue;

                    Color tint = Color.White * Brightness(map, p);
                    var destination = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                    Tile tile = map.GetTile(p);

                    switch (tile.Type)
                    {
                        case TileType.Wall:
                            // Autotile-lite: walls above open floor show their front face.
                            bool faces = map.IsWalkable(new Point(x, y + 1));
                            spriteBatch.Draw(faces ? wallFaceTex : wallTex, destination, tint);
                            break;
                        case TileType.Stairs:
                            spriteBatch.Draw(floorTex, destination, tint);
                            spriteBatch.Draw(stairsTex, destination, tint);
                            break;
                        case TileType.Shopkeeper:
                            spriteBatch.Draw(floorTex, destination, tint);
                            spriteBatch.Draw(keeperTex, destination, tint);
                            break;
                        default:
                            spriteBatch.Draw(floorTex, destination, tint);
                            break;
                    }
                }
            }

            // Ground items: tier icons on tiles.
            foreach (Items.GroundItem ground in map.GroundItems)
            {
                if (!map.IsExplored(ground.Position)) continue;
                DrawIcon(spriteBatch, ground.Item.Tier, ground.Position, tileSize, Brightness(map, ground.Position));
            }

            // Money piles: coin sprite.
            Texture2D coin = _content.GetTexture("Props/coin");
            foreach (Items.MoneyPile pile in map.MoneyPiles)
            {
                if (!map.IsExplored(pile.Position)) continue;
                var dest = new Rectangle(pile.Position.X * tileSize + tileSize / 2 - 8,
                    pile.Position.Y * tileSize + tileSize / 2 - 8, 16, 16);
                spriteBatch.Draw(coin, dest, Color.White * Brightness(map, pile.Position));
            }

            // Chests.
            Texture2D chestTex = _content.GetTexture("Props/chest");
            foreach (Items.Chest chest in map.Chests)
            {
                if (!map.IsExplored(chest.Position)) continue;
                var dest = new Rectangle(chest.Position.X * tileSize, chest.Position.Y * tileSize, tileSize, tileSize);
                spriteBatch.Draw(chestTex, dest, Color.White * Brightness(map, chest.Position));
            }

            // Shop stock: white "for sale" ring under the tier icon.
            foreach (Items.ShopItem shopItem in map.ShopItems)
            {
                if (!map.IsExplored(shopItem.Position)) continue;
                float b = Brightness(map, shopItem.Position);
                int px = shopItem.Position.X * tileSize;
                int py = shopItem.Position.Y * tileSize;
                spriteBatch.Draw(pixel, new Rectangle(px + tileSize / 2 - 11, py + tileSize / 2 - 11, 22, 22),
                    Color.White * (0.85f * b));
                spriteBatch.Draw(floorTex, new Rectangle(px + tileSize / 2 - 9, py + tileSize / 2 - 9, 18, 18),
                    Color.White * b);
                DrawIcon(spriteBatch, shopItem.Item.Tier, shopItem.Position, tileSize, b);
            }

            // Actors only exist while in sight.
            Texture2D marker = _content.GetTexture("Props/marker_player");
            foreach (Actor actor in map.Actors)
            {
                if (!map.IsVisible(actor.GridPosition)) continue;

                Texture2D sheet = _content.GetTexture(actor.SpriteKey);
                Vector2 drawPos = actor.RenderPosition + actor.VisualOffset;
                var destination = new Rectangle((int)drawPos.X, (int)drawPos.Y, tileSize, tileSize);
                Rectangle source = SpriteSheets.Source(actor.Facing, actor.IsMoving, totalMs);

                if (actor is Boss)
                {
                    // Gold platform glow so the boss reads as special.
                    spriteBatch.Draw(pixel, new Rectangle(destination.X - 2, destination.Y - 2,
                        tileSize + 4, tileSize + 4), new Color(235, 190, 80) * 0.55f);
                }

                spriteBatch.Draw(sheet, destination, source, Color.White);

                if (actor is Player)
                {
                    spriteBatch.Draw(marker, destination, Color.White * 0.85f);
                }

                // Hit flash overlay.
                if (actor.HitFlash > 0f)
                {
                    spriteBatch.Draw(pixel, destination, Color.White * (actor.HitFlash * 0.6f));
                }

                // Status indicator: small colored square in the tile corner.
                if (actor.StatusType != Combat.StatusType.None)
                {
                    var dot = new Rectangle(destination.X + tileSize - 9, destination.Y + 1, 8, 8);
                    spriteBatch.Draw(pixel, dot, HudRenderer.StatusColor(actor.StatusType));
                }
            }
        }

        private void DrawIcon(SpriteBatch spriteBatch, Items.ItemTier tier, Point tile, int tileSize, float brightness)
        {
            Texture2D icon = _content.GetTexture($"Icons/{tier.ToString().ToLowerInvariant()}");
            var dest = new Rectangle(tile.X * tileSize + tileSize / 2 - 8,
                tile.Y * tileSize + tileSize / 2 - 8, 16, 16);
            spriteBatch.Draw(icon, dest, Color.White * brightness);
        }

        private static float Brightness(DungeonMap map, Point p) => map.IsVisible(p) ? 1f : DimFactor;
    }
}
