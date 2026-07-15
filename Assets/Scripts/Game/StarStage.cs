using UnityEngine;

namespace Universes.Game
{
    public enum StarStage
    {
        Yellow,
        Orange,
        RedGiant,
        Supernova
    }

    public static class StarStageUtility
    {
        public static StarStage FromAge(int age, int maxAge = 100)
        {
            maxAge = Mathf.Max(1, maxAge);
            if (age >= maxAge) return StarStage.Supernova;

            var ratio = (float)age / maxAge;
            if (ratio >= 0.67f) return StarStage.RedGiant;
            if (ratio >= 0.34f) return StarStage.Orange;
            return StarStage.Yellow;
        }

        public static int GetClickReward(StarStage stage) =>
            stage switch
            {
                StarStage.Yellow => 1,
                StarStage.Orange => 3,
                StarStage.RedGiant => 8,
                _ => 0
            };

        public static int GetPassivePerSecond(StarStage stage) =>
            stage switch
            {
                StarStage.Yellow => 1,
                StarStage.Orange => 3,
                StarStage.RedGiant => 8,
                _ => 0
            };
    }
}
