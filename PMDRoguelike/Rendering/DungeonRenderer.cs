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
            foreach (Actor actor in map.Actors)
            {
                Texture2D texture = _content.GetTexture(actor.SpriteKey);
                Vector2 drawPos = actor.RenderPosition + actor.VisualOffset;

                // The player gets a white outline so they stand out among species colors.
                if (actor is Player)
                {
                    spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X, (int)drawPos.Y, tileSize, tileSize),
                        Color.White * 0.9f);
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
            }
        }
    }
}
