namespace Universes.Game
{
    public class StarSystemBreakdown
    {
        public int FromDnaPotential;
        public int FromCivilizationBonus;
        public int FromPlanetBonus;
        public int TotalGained;

        public static StarSystemBreakdown Calculate(RunStats stats, double dnaPotential,
            int highestCivRank)
        {
            var breakdown = new StarSystemBreakdown
            {
                FromDnaPotential = UnityEngine.Mathf.FloorToInt((float)dnaPotential),
                FromCivilizationBonus = highestCivRank,
                FromPlanetBonus = stats.PlanetsCreated / 2
            };

            breakdown.TotalGained = breakdown.FromDnaPotential +
                                    breakdown.FromCivilizationBonus +
                                    breakdown.FromPlanetBonus;

            if (breakdown.TotalGained < 1 && stats.TotalStardustProduced > 0)
                breakdown.TotalGained = 1;

            return breakdown;
        }
    }
}
