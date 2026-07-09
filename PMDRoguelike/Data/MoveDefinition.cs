namespace PMDRoguelike.Data
{
    public enum MoveCategory
    {
        Physical,
        Special,
        /// <summary>No damage; only applies its status effect.</summary>
        Status
    }

    public enum MoveRange
    {
        /// <summary>Hits the adjacent tile the user is facing (8 directions, corner rules apply).</summary>
        Melee,
        /// <summary>Travels in a straight line along facing until it hits an actor or a wall.</summary>
        Line
    }

    /// <summary>
    /// Static definition of a move, authored in Content/Data/Moves.json.
    /// Runtime PP lives in <see cref="Combat.MoveSlot"/>.
    /// </summary>
    public class MoveDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PokemonType Type { get; set; }
        public MoveCategory Category { get; set; }
        public int Power { get; set; }
        /// <summary>Hit chance in percent (100 = never misses in practice).</summary>
        public int Accuracy { get; set; } = 100;
        public int PP { get; set; }
        public MoveRange Range { get; set; } = MoveRange.Melee;
        /// <summary>Max tiles a Line move travels. Ignored for Melee.</summary>
        public int Distance { get; set; } = 1;

        /// <summary>Status this move can inflict (None = never). Status-category moves always try.</summary>
        public Combat.StatusType InflictStatus { get; set; } = Combat.StatusType.None;
        /// <summary>Percent chance to inflict when the move connects.</summary>
        public int InflictChance { get; set; }
    }
}
