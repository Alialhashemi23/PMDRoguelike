using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PMDRoguelike.Entities;
using PMDRoguelike.Rendering;

namespace PMDRoguelike.States
{
    /// <summary>Shared run-stats block for the game-over and victory screens.</summary>
    public static class RunSummary
    {
        public static void Draw(SpriteBatch spriteBatch, PMDRogueGame game, Player player, int totalTurns,
            float startYOffset)
        {
            if (player == null) return;

            var stats = player.RunStats;
            string[] lines =
            {
                $"Level {player.Level} {player.Species.Name}  —  {totalTurns} turns",
                $"Defeated: {stats.Kills} Pokémon ({stats.BossesDefeated} bosses)",
                $"Damage dealt: {stats.DamageDealt}   taken: {stats.DamageTaken}",
                $"Items collected: {stats.ItemsCollected}   Poké earned: {stats.PokeEarned}"
            };

            float y = startYOffset;
            foreach (string line in lines)
            {
                TextRenderer.DrawCentered(spriteBatch, game, line, y, new Color(200, 200, 210), 0.85f);
                y += 32;
            }
        }
    }
}
