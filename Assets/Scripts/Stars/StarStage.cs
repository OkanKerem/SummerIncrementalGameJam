namespace Universes.Stars
{
    public enum StarStage
    {
        Yellow,
        Orange,
        RedGiant,
        Supernova,
        Dead
    }

    public static class StarStageUtility
    {
        public static StarStage FromAge(float age)
        {
            if (age >= 100f) return StarStage.Dead;
            if (age >= 90f) return StarStage.Supernova;
            if (age >= 60f) return StarStage.RedGiant;
            if (age >= 30f) return StarStage.Orange;
            return StarStage.Yellow;
        }

        public static bool AllowsPlanets(StarStage stage) =>
            stage == StarStage.Yellow || stage == StarStage.Orange;

        public static double GetProductionMultiplier(StarStage stage, Core.GameBalance balance)
        {
            return stage switch
            {
                StarStage.Yellow => balance.yellowProductionMultiplier,
                StarStage.Orange => balance.orangeProductionMultiplier,
                StarStage.RedGiant => balance.redGiantProductionMultiplier,
                StarStage.Supernova => balance.supernovaProductionMultiplier,
                _ => 0
            };
        }
    }
}
