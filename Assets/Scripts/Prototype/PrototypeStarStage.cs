using UnityEngine;

namespace Universes.Prototype
{
    public enum PrototypeStarStage
    {
        Yellow,
        Orange,
        RedGiant,
        Supernova
    }

    public static class PrototypeStarStageUtility
    {
        public static PrototypeStarStage FromAge(int age, int maxAge = 100)
        {
            maxAge = Mathf.Max(1, maxAge);
            if (age >= maxAge) return PrototypeStarStage.Supernova;

            var ratio = (float)age / maxAge;
            if (ratio >= 0.67f) return PrototypeStarStage.RedGiant;
            if (ratio >= 0.34f) return PrototypeStarStage.Orange;
            return PrototypeStarStage.Yellow;
        }

        public static int GetClickReward(PrototypeStarStage stage) =>
            stage switch
            {
                PrototypeStarStage.Yellow => 1,
                PrototypeStarStage.Orange => 3,
                PrototypeStarStage.RedGiant => 8,
                _ => 0
            };

        public static int GetPassivePerSecond(PrototypeStarStage stage) =>
            stage switch
            {
                PrototypeStarStage.Yellow => 1,
                PrototypeStarStage.Orange => 3,
                PrototypeStarStage.RedGiant => 8,
                _ => 0
            };
    }
}
