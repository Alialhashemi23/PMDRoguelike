using PMDRoguelike.Data;

namespace PMDRoguelike.Combat
{
    /// <summary>A known move plus its remaining PP.</summary>
    public class MoveSlot
    {
        public MoveDefinition Move { get; }
        public int CurrentPP { get; set; }

        public MoveSlot(MoveDefinition move)
        {
            Move = move;
            CurrentPP = move.PP;
        }

        public bool HasPP => CurrentPP > 0;
    }
}
