namespace PMDRoguelike.Run
{
    /// <summary>Run-wide tallies shown on the game-over and victory screens.</summary>
    public class RunStats
    {
        public int Kills { get; set; }
        public int BossesDefeated { get; set; }
        public int DamageDealt { get; set; }
        public int DamageTaken { get; set; }
        public int ItemsCollected { get; set; }
        public int PokeEarned { get; set; }
    }
}
