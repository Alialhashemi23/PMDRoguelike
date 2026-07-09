namespace PMDRoguelike.Dungeon
{
    public enum TileType
    {
        Wall,
        Floor,
        Stairs,
        /// <summary>Shop-room keeper: blocks movement, purely decorative otherwise.</summary>
        Shopkeeper
        // Reserved for later phases: Water, Trap
    }
}
