using System;
using Universes.Core;
using Universes.Planets;
using Universes.Prestige;
using Universes.Stars;

namespace Universes.Core
{
    [Serializable]
    public class CollapseBreakdown
    {
        public double stardustComponent;
        public double starsComponent;
        public double planetsComponent;
        public double lifeComponent;
        public double supernovaComponent;
        public double survivalComponent;
        public double blackHoleComponent;
        public double rawScore;
        public double multiplier;
        public double dnaGained;
        public bool earlyCollapse;
    }

    public static class CollapseCalculator
    {
        public static CollapseBreakdown Calculate(
            RunStats stats,
            GameBalance balance,
            RunModifiers modifiers,
            float entropy,
            bool manualCollapse)
        {
            var breakdown = new CollapseBreakdown
            {
                stardustComponent = Math.Sqrt(stats.TotalStardustProduced) * balance.dnaK1StardustSqrt,
                starsComponent = stats.StarsCreated * balance.dnaK2Stars,
                planetsComponent = stats.PlanetsCreated * balance.dnaK3Planets,
                lifeComponent = stats.LifePlanets * balance.dnaK4LifePlanets,
                supernovaComponent = stats.SupernovaCount * balance.dnaK5Supernovas,
                survivalComponent = stats.RunDurationSeconds * balance.dnaK6SurvivalTime,
                blackHoleComponent = stats.BlackHoleSeedCount * balance.dnaK7BlackHoleSeeds
            };

            breakdown.rawScore = breakdown.stardustComponent
                + breakdown.starsComponent
                + breakdown.planetsComponent
                + breakdown.lifeComponent
                + breakdown.supernovaComponent
                + breakdown.survivalComponent
                + breakdown.blackHoleComponent;

            breakdown.multiplier = modifiers.DnaMultiplier * modifiers.VariantDnaMultiplier;
            breakdown.earlyCollapse = manualCollapse && entropy < balance.earlyCollapseEntropyThreshold;

            if (breakdown.earlyCollapse)
                breakdown.multiplier *= balance.earlyCollapsePenalty;

            breakdown.dnaGained = Math.Floor(breakdown.rawScore * breakdown.multiplier);
            return breakdown;
        }
    }

    [Serializable]
    public class RunStats
    {
        public double TotalStardustProduced;
        public int StarsCreated;
        public int PlanetsCreated;
        public int LifePlanets;
        public int SupernovaCount;
        public int BlackHoleSeedCount;
        public double RunDurationSeconds;
    }
}
