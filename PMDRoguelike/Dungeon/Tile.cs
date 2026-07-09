namespace PMDRoguelike.Dungeon
{
    public struct Tile
    {
        public TileType Type;

        /// <summary>
        /// Logical texture key resolved by the renderer/content manager,
        /// so real sprite sheets can replace placeholders without touching map logic.
        /// </summary>
        public string SpriteKey;

        public Tile(TileType type)
        {
            Type = type;
            SpriteKey = type == TileType.Wall ? "tile.wall" : "tile.floor";
        }

        public readonly bool IsWalkable => Type == TileType.Floor;
    }
}
