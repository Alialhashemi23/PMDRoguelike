using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PMDRoguelike.Core;
using PMDRoguelike.Data;
using PMDRoguelike.Managers;
using PMDRoguelike.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace PMDRoguelike.States
{
    /// <summary>Pick a starter: browse the four candidates with their stats and moves.</summary>
    public class StarterSelectState : GameState
    {
        private static readonly string[] StarterIds = { "bulbasaur", "charmander", "squirtle", "pikachu" };
        private const int StartLevel = 5;

        private int _selected = 1; // Charmander by default, as tradition demands

        public StarterSelectState(PMDRogueGame game) : base(game) { }

        public override void Update(GameTime gameTime)
        {
            KeyboardManager keyboard = KeyboardManager.Instance;

            if (keyboard.WasKeyJustPressed(Keys.Left) || keyboard.WasKeyJustPressed(Keys.A))
            {
                _selected = (_selected + StarterIds.Length - 1) % StarterIds.Length;
                Core.AudioCues.Post("menu");
            }
            if (keyboard.WasKeyJustPressed(Keys.Right) || keyboard.WasKeyJustPressed(Keys.D))
            {
                _selected = (_selected + 1) % StarterIds.Length;
                Core.AudioCues.Post("menu");
            }

            if (keyboard.WasKeyJustPressed(Keys.Enter) || keyboard.WasKeyJustPressed(Keys.Z))
            {
                Core.AudioCues.Post("levelup");
                Game.States.ChangeState(new DungeonState(Game, StarterIds[_selected]));
            }
            if (keyboard.WasKeyJustPressed(Keys.Escape))
                Game.States.ChangeState(new TitleState(Game));
        }

        public override void Draw(GameTime gameTime)
        {
            Game.GraphicsDevice.Clear(new Color(16, 18, 28));

            SpriteBatch spriteBatch = Game.SpriteBatch;
            SpriteFont font = Game.GameContent.LoadFont("Default");
            var viewport = Game.GraphicsDevice.Viewport;

            spriteBatch.Begin();
            TextRenderer.DrawCentered(spriteBatch, Game, "Choose your Pokémon", -260, new Color(255, 210, 100), 1.4f);
            TextRenderer.DrawCentered(spriteBatch, Game, "Left/Right to browse — Enter to set out", -210, Color.Gray, 0.85f);

            // The four candidates as colored squares, selection highlighted.
            int cell = 84;
            int totalWidth = StarterIds.Length * cell + (StarterIds.Length - 1) * 30;
            int x0 = (viewport.Width - totalWidth) / 2;
            int y0 = viewport.Height / 2 - 150;

            for (int i = 0; i < StarterIds.Length; i++)
            {
                SpeciesDefinition species = GameData.GetSpecies(StarterIds[i]);
                Game.GameContent.RegisterSolid($"species.{species.Id}", ColorUtil.FromHex(species.Color));
                Texture2D swatch = Game.GameContent.GetTexture($"species.{species.Id}");
                Texture2D pixel = Game.GameContent.GetTexture("ui.pixel");

                int x = x0 + i * (cell + 30);
                if (i == _selected)
                {
                    spriteBatch.Draw(pixel, new Rectangle(x - 6, y0 - 6, cell + 12, cell + 12), Color.White);
                }
                spriteBatch.Draw(swatch, new Rectangle(x, y0, cell, cell), Color.White);

                if (font != null)
                {
                    Vector2 nameSize = font.MeasureString(species.Name) * 0.8f;
                    TextRenderer.DrawShadowed(spriteBatch, font, species.Name,
                        new Vector2(x + (cell - nameSize.X) / 2f, y0 + cell + 8),
                        i == _selected ? Color.White : Color.Gray, 0.8f);
                }
            }

            DrawSelectedDetails(spriteBatch, font, viewport.Width, y0 + cell + 60);
            spriteBatch.End();
        }

        private void DrawSelectedDetails(SpriteBatch spriteBatch, SpriteFont font, int viewportWidth, int y)
        {
            if (font == null) return;

            SpeciesDefinition species = GameData.GetSpecies(StarterIds[_selected]);
            StatBlock stats = StatBlock.AtLevel(species.BaseStats, StartLevel);
            List<string> moves = species.Learnset
                .Where(e => e.Level <= StartLevel)
                .TakeLast(4)
                .Select(e => GameData.GetMove(e.Move).Name)
                .ToList();

            string types = string.Join(" / ", species.Types);
            string[] lines =
            {
                $"{species.Name}  —  {types}",
                $"Lv.{StartLevel}:  HP {stats.HP}   Atk {stats.Attack}   Def {stats.Defense}   SpA {stats.SpAttack}   SpD {stats.SpDefense}   Spe {stats.Speed}",
                $"Moves: {string.Join(", ", moves)}"
            };

            foreach (string line in lines)
            {
                Vector2 size = font.MeasureString(line) * 0.85f;
                TextRenderer.DrawShadowed(spriteBatch, font, line,
                    new Vector2((viewportWidth - size.X) / 2f, y), new Color(200, 200, 210), 0.85f);
                y += 34;
            }
        }
    }
}
