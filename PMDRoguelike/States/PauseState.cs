using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;

namespace PMDRoguelike.States
{
    /// <summary>
    /// Pause overlay pushed on top of the DungeonState: resume, controls reference,
    /// or abandon the run (which ends it permadeath-style).
    /// </summary>
    public class PauseState : GameState
    {
        private static readonly string[] Controls =
        {
            "Arrows / WASD — move (8 directions, diagonals can't cut corners)",
            "1-4 — use a move along your facing (bump to turn for free)",
            "Space — wait a turn",
            "Q / E — trigger active items",
            "Hold Shift — moves panel    Hold Tab — items panel",
            "Enter / Z — interact: descend stairs, open chests, buy",
            "Esc — pause / resume"
        };

        private readonly Player _player;
        private readonly int _totalTurns;

        public PauseState(PMDRogueGame game, Player player, int totalTurns) : base(game)
        {
            _player = player;
            _totalTurns = totalTurns;
        }

        public override void Enter() => Core.AudioCues.Post("menu");

        public override void Update(GameTime gameTime)
        {
            KeyboardManager keyboard = KeyboardManager.Instance;

            if (keyboard.WasKeyJustPressed(Keys.Escape) || keyboard.WasKeyJustPressed(Keys.Enter))
            {
                Core.AudioCues.Post("menu");
                Game.States.Pop();
                return;
            }

            if (keyboard.WasKeyJustPressed(Keys.M)) Game.Audio?.ToggleMute();
            if (keyboard.WasKeyJustPressed(Keys.OemMinus)) Game.Audio?.AdjustMasterVolume(-0.1f);
            if (keyboard.WasKeyJustPressed(Keys.OemPlus)) Game.Audio?.AdjustMasterVolume(0.1f);

            if (keyboard.WasKeyJustPressed(Keys.X))
            {
                Game.States.ChangeState(new GameOverState(Game, "You abandoned the run.", _player, _totalTurns));
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Game.GraphicsDevice.Clear(new Color(12, 12, 20));

            var spriteBatch = Game.SpriteBatch;
            spriteBatch.Begin();

            TextRenderer.DrawCentered(spriteBatch, Game, "PAUSED", -220, new Color(255, 210, 100), 1.5f);

            float y = -140;
            foreach (string line in Controls)
            {
                TextRenderer.DrawCentered(spriteBatch, Game, line, y, new Color(190, 190, 200), 0.85f);
                y += 34;
            }

            string mute = Game.Audio?.Muted == true ? "muted" : $"{(int)((Game.Audio?.MasterVolume ?? 0f) * 100)}%";
            TextRenderer.DrawCentered(spriteBatch, Game, $"M — mute    - / + — volume ({mute})", y + 10,
                new Color(150, 160, 180), 0.85f);
            TextRenderer.DrawCentered(spriteBatch, Game, "Esc / Enter — resume        X — abandon run", y + 50,
                Color.Gray, 0.9f);

            spriteBatch.End();
        }
    }
}
