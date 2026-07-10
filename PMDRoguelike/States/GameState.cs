using Microsoft.Xna.Framework;

namespace PMDRoguelike.States
{
    /// <summary>
    /// One screen/mode of the game (dungeon crawl, game over, victory, later: title,
    /// menus). The game class delegates Update/Draw to whichever state is active.
    /// </summary>
    public abstract class GameState
    {
        protected readonly PMDRogueGame Game;

        protected GameState(PMDRogueGame game)
        {
            Game = game;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }

        public abstract void Update(GameTime gameTime);
        public abstract void Draw(GameTime gameTime);
    }

    public class GameStateManager
    {
        private readonly System.Collections.Generic.Stack<GameState> _stack = new();

        public GameState Current => _stack.Count > 0 ? _stack.Peek() : null;

        /// <summary>Replace the whole stack (screen transitions).</summary>
        public void ChangeState(GameState next)
        {
            while (_stack.Count > 0) _stack.Pop().Exit();
            _stack.Push(next);
            next.Enter();
        }

        /// <summary>Overlay a state (pause) without disturbing the one beneath.</summary>
        public void Push(GameState state)
        {
            _stack.Push(state);
            state.Enter();
        }

        /// <summary>Close the top overlay and resume the state beneath it.</summary>
        public void Pop()
        {
            if (_stack.Count > 0) _stack.Pop().Exit();
        }
    }
}
