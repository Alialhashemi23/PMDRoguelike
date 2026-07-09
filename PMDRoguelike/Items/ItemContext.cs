using PMDRoguelike.Combat;
using PMDRoguelike.Core;
using PMDRoguelike.Dungeon;
using PMDRoguelike.Entities;
using PMDRoguelike.UI;

namespace PMDRoguelike.Items
{
    /// <summary>
    /// Everything an item effect may need when it fires. Built by the CombatResolver
    /// (which owns the authoritative one per floor).
    /// </summary>
    public class ItemContext
    {
        public Player Player { get; init; }
        public DungeonMap Map { get; init; }
        public MessageLog Log { get; init; }
        public Rng Rng { get; init; }
        public CombatResolver Combat { get; init; }

        public Inventory Inventory => Player.Inventory;
    }
}
