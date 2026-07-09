using Microsoft.Xna.Framework;

namespace PMDRoguelike.Items
{
    /// <summary>An item lying on a dungeon tile, picked up by walking over it.</summary>
    public class GroundItem
    {
        public Point Position { get; }
        public Item Item { get; }

        public GroundItem(Point position, Item item)
        {
            Position = position;
            Item = item;
        }
    }
}
