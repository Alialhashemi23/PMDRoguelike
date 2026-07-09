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
}
