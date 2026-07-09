using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PMDRoguelike.Entities;
using PMDRoguelike.Managers;
using PMDRoguelike.Run;

namespace PMDRoguelike.Rendering
{
    /// <summary>
    /// In-dungeon overlay: location, player vitals (HP bar, level, EXP), turn count,
    /// and the descend prompt. Drawn in screen space (no camera transform).
    /// </summary>
    public class HudRenderer
    {
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        public HudRenderer(GameContentManager content)
        {
            _font = content.LoadFont("Default");
            content.RegisterSolid("ui.pixel", Color.White);
            _pixel = content.GetTexture("ui.pixel");
        }

        public void Draw(SpriteBatch spriteBatch, RunManager run, Player player, int floorTurns,
            string prompt, int viewportWidth, int viewportHeight)
        {
            if (_font == null) return;

            string location = $"{run.CurrentDungeon.Name}  —  F{run.FloorNumber}/{run.CurrentDungeon.Floors}";
            TextRenderer.DrawShadowed(spriteBatch, _font, location, new Vector2(12, 8), Color.White);

            DrawPlayerVitals(spriteBatch, player);

            string turns = $"Turns: {run.TotalTurns + floorTurns}";
            Vector2 turnsSize = _font.MeasureString(turns);
            TextRenderer.DrawShadowed(spriteBatch, _font, turns,
                new Vector2(viewportWidth - turnsSize.X - 12, 8), new Color(200, 200, 200));

            string poke = $"Poké: {player.Poke}";
            Vector2 pokeSize = _font.MeasureString(poke) * 0.8f;
            TextRenderer.DrawShadowed(spriteBatch, _font, poke,
                new Vector2(viewportWidth - pokeSize.X - 12, 64), new Color(235, 200, 90), 0.8f);

            TextRenderer.DrawShadowed(spriteBatch, _font, "Shift: moves   Tab: items",
                new Vector2(viewportWidth - 246, 40), new Color(140, 140, 150), 0.7f);

            // Active item slots, bottom-right.
            for (int i = 0; i < Items.Inventory.MaxActiveSlots; i++)
            {
                string key = i == 0 ? "Q" : "E";
                string label = i < player.Inventory.Actives.Count
                    ? $"[{key}] {player.Inventory.Actives[i].Name}"
                    : $"[{key}] —";
                Color color = i < player.Inventory.Actives.Count
                    ? Items.Item.TierColor(Items.ItemTier.Active)
                    : new Color(110, 110, 120);
                Vector2 size = _font.MeasureString(label) * 0.7f;
                TextRenderer.DrawShadowed(spriteBatch, _font, label,
                    new Vector2(viewportWidth - size.X - 12, viewportHeight - 30 - i * 22), color, 0.7f);
            }

            if (prompt != null)
            {
                Vector2 promptSize = _font.MeasureString(prompt);
                TextRenderer.DrawShadowed(spriteBatch, _font, prompt,
                    new Vector2((viewportWidth - promptSize.X) / 2f, viewportHeight - promptSize.Y - 140),
                    new Color(150, 190, 255));
            }
        }

        /// <summary>Big red boss bar, top-center, while the boss lives.</summary>
        public void DrawBossBar(SpriteBatch spriteBatch, Boss boss, int viewportWidth)
        {
            if (_font == null) return;

            const int barWidth = 420, barHeight = 14;
            int barX = (viewportWidth - barWidth) / 2;
            const int barY = 46;

            Vector2 nameSize = _font.MeasureString(boss.DisplayName) * 0.8f;
            TextRenderer.DrawShadowed(spriteBatch, _font, boss.DisplayName,
                new Vector2((viewportWidth - nameSize.X) / 2f, barY - 28), new Color(240, 120, 120), 0.8f);

            float pct = boss.Stats.HP > 0 ? (float)boss.CurrentHP / boss.Stats.HP : 0f;
            spriteBatch.Draw(_pixel, new Rectangle(barX - 1, barY - 1, barWidth + 2, barHeight + 2), Color.Black * 0.7f);
            spriteBatch.Draw(_pixel, new Rectangle(barX, barY, barWidth, barHeight), new Color(60, 40, 40));
            spriteBatch.Draw(_pixel, new Rectangle(barX, barY, (int)(barWidth * pct), barHeight), new Color(210, 70, 70));
        }

        private void DrawPlayerVitals(SpriteBatch spriteBatch, Player player)
        {
            TextRenderer.DrawShadowed(spriteBatch, _font,
                $"Lv.{player.Level} {player.DisplayName}", new Vector2(12, 40), Color.White, 0.8f);

            const int barX = 12, barY = 68, barWidth = 180, barHeight = 12;
            float pct = player.Stats.HP > 0 ? (float)player.CurrentHP / player.Stats.HP : 0f;
            Color fill = pct > 0.5f ? new Color(96, 200, 96)
                : pct > 0.25f ? new Color(230, 200, 80)
                : new Color(220, 90, 90);

            spriteBatch.Draw(_pixel, new Rectangle(barX - 1, barY - 1, barWidth + 2, barHeight + 2), Color.Black * 0.7f);
            spriteBatch.Draw(_pixel, new Rectangle(barX, barY, barWidth, barHeight), new Color(60, 60, 66));
            spriteBatch.Draw(_pixel, new Rectangle(barX, barY, (int)(barWidth * pct), barHeight), fill);

            TextRenderer.DrawShadowed(spriteBatch, _font, $"{player.CurrentHP}/{player.Stats.HP}",
                new Vector2(barX + barWidth + 8, barY - 4), Color.White, 0.7f);

            if (player.StatusType != Combat.StatusType.None)
            {
                TextRenderer.DrawShadowed(spriteBatch, _font, Combat.StatusRules.Abbreviation(player.StatusType),
                    new Vector2(barX + barWidth + 74, barY - 4), StatusColor(player.StatusType), 0.7f);
            }

            TextRenderer.DrawShadowed(spriteBatch, _font, $"EXP {player.Exp}/{player.ExpToNextLevel}",
                new Vector2(barX, barY + 16), new Color(170, 170, 180), 0.65f);
        }

        /// <summary>Shared placeholder colors for status conditions (HUD + map dots).</summary>
        public static Color StatusColor(Combat.StatusType type) => type switch
        {
            Combat.StatusType.Burn => new Color(240, 128, 48),
            Combat.StatusType.Poison => new Color(160, 64, 160),
            Combat.StatusType.Paralysis => new Color(248, 208, 48),
            Combat.StatusType.Sleep => new Color(140, 136, 192),
            _ => Color.White
        };
    }
}
