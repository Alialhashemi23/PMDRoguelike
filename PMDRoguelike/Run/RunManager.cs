using PMDRoguelike.Dungeon;
using System.Collections.Generic;

namespace PMDRoguelike.Run
{
    public enum AdvanceResult
    {
        NextFloor,
        NextDungeon,
        Victory
    }

    /// <summary>
    /// Tracks where the player is in the overall run: which dungeon, which floor,
    /// and simple run-wide stats. Owns the floor→dungeon→victory progression logic.
    /// </summary>
    public class RunManager
    {
        private readonly IReadOnlyList<DungeonDefinition> _dungeons;

        public int DungeonIndex { get; private set; }

        /// <summary>1-based floor number within the current dungeon.</summary>
        public int FloorNumber { get; private set; } = 1;

        /// <summary>Turns accumulated on completed floors (add the live floor's count for display).</summary>
        public int TotalTurns { get; private set; }

        public DungeonDefinition CurrentDungeon => _dungeons[DungeonIndex];

        public bool IsFinalFloorOfDungeon => FloorNumber >= CurrentDungeon.Floors;

        public RunManager(IReadOnlyList<DungeonDefinition> dungeons)
        {
            _dungeons = dungeons;
        }

        public void AddTurns(int turns) => TotalTurns += turns;

        /// <summary>Advance past the current floor's stairs.</summary>
        public AdvanceResult Advance()
        {
            if (FloorNumber < CurrentDungeon.Floors)
            {
                FloorNumber++;
                return AdvanceResult.NextFloor;
            }

            if (DungeonIndex < _dungeons.Count - 1)
            {
                DungeonIndex++;
                FloorNumber = 1;
                return AdvanceResult.NextDungeon;
            }

            return AdvanceResult.Victory;
        }
    }
}
