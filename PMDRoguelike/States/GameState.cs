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
        public GameState Current { get; private set; }

        public void ChangeState(GameState next)
        {
            Current?.Exit();
            Current = next;
            Current?.Enter();
        }
    }
}
