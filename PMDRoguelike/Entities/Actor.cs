using Microsoft.Xna.Framework;
using PMDRoguelike.Core;

namespace PMDRoguelike.Entities
{
    /// <summary>
    /// An entity that participates in the turn order (player, enemies).
    /// Stats are placeholders until the combat phase.
    /// </summary>
    public abstract class Actor : Entity
    {
        public Direction Facing { get; set; } = Direction.South;

        // Placeholder stat block — replaced by full Pokémon stats in the combat phase.
        public int Level { get; set; } = 1;
        public int Speed { get; set; } = 10;

        protected Actor(Point gridPosition) : base(gridPosition) { }
    }
}
