using PMDRoguelike.Core;

namespace PMDRoguelike.Turns
{
    /// <summary>
    /// A single actor's chosen action for one turn.
    /// Attack/UseItem actions join this hierarchy in later phases.
    /// </summary>
    public abstract class TurnAction
    {
    }

    public sealed class MoveAction : TurnAction
    {
        public Direction Direction { get; }

        public MoveAction(Direction direction)
        {
            Direction = direction;
        }
    }

    public sealed class WaitAction : TurnAction
    {
    }

    /// <summary>Use the move in the given slot (0-3) along the actor's facing.</summary>
    public sealed class AttackAction : TurnAction
    {
        public int MoveIndex { get; }

        public AttackAction(int moveIndex)
        {
            MoveIndex = moveIndex;
        }
    }
}
