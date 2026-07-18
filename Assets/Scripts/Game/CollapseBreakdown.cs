using UnityEngine;

namespace Universes.Game
{
    public class CollapseBreakdown
    {
        public int BaseReward;
        public int FromDnaFragments;
        public int FromDnaPotential;
        public int FromSupernovas;
        public int FromCollisions;
        public int FromProduction;
        public int FromLifetime;
        public int TotalGained;

        public static CollapseBreakdown Calculate(RunStats stats, double dnaPotential)
        {
            var breakdown = new CollapseBreakdown
            {
                BaseReward = PrestigeBalance.BaseCollapseDnaReward,
                FromDnaFragments = stats.DnaFragmentsCollected,
                FromDnaPotential = Mathf.FloorToInt((float)dnaPotential),
                FromSupernovas = stats.SupernovaCount / PrestigeBalance.SupernovasPerDnaPoint,
                FromCollisions = stats.StarCollisionCount / PrestigeBalance.CollisionsPerDnaPoint,
                FromProduction = (int)(stats.TotalStardustProduced / PrestigeBalance.StardustPerDnaPoint),
                FromLifetime = Mathf.FloorToInt(stats.SurvivalTimeSeconds /
                                                PrestigeBalance.SecondsPerLifetimeDnaPoint)
            };

            breakdown.TotalGained = breakdown.BaseReward
                                      + breakdown.FromDnaFragments
                                      + breakdown.FromDnaPotential
                                      + breakdown.FromSupernovas
                                      + breakdown.FromCollisions
                                      + breakdown.FromProduction
                                      + breakdown.FromLifetime;

            return breakdown;
        }
    }
}
