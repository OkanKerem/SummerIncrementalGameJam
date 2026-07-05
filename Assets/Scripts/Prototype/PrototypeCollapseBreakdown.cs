using UnityEngine;

namespace Universes.Prototype
{
    public class PrototypeCollapseBreakdown
    {
        public int BaseReward;
        public int FromDnaFragments;
        public int FromBlackHolePotential;
        public int FromSupernovas;
        public int FromCollisions;
        public int FromProduction;
        public int FromLifetime;
        public int TotalGained;

        public static PrototypeCollapseBreakdown Calculate(PrototypeRunStats stats)
        {
            var breakdown = new PrototypeCollapseBreakdown
            {
                BaseReward = PrototypePrestigeBalance.BaseCollapseDnaReward,
                FromDnaFragments = stats.DnaFragmentsCollected,
                FromBlackHolePotential = Mathf.FloorToInt(stats.BlackHoleDnaPotential),
                FromSupernovas = stats.SupernovaCount / PrototypePrestigeBalance.SupernovasPerDnaPoint,
                FromCollisions = stats.StarCollisionCount / PrototypePrestigeBalance.CollisionsPerDnaPoint,
                FromProduction = (int)(stats.TotalStardustProduced / PrototypePrestigeBalance.StardustPerDnaPoint),
                FromLifetime = Mathf.FloorToInt(stats.SurvivalTimeSeconds /
                                                PrototypePrestigeBalance.SecondsPerLifetimeDnaPoint)
            };

            breakdown.TotalGained = breakdown.BaseReward
                                      + breakdown.FromDnaFragments
                                      + breakdown.FromBlackHolePotential
                                      + breakdown.FromSupernovas
                                      + breakdown.FromCollisions
                                      + breakdown.FromProduction
                                      + breakdown.FromLifetime;

            return breakdown;
        }
    }
}
