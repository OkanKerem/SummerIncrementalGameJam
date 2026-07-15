namespace Universes.Stars
{
    public enum O_StarStage
    {
        Yellow,
        Orange,
        RedGiant,
        Supernova,
        Dead
    }

    public static class O_StarStageUtility
    {
        public static O_StarStage FromAge(float age)
        {
            if (age >= 100f) return O_StarStage.Dead;
            if (age >= 90f) return O_StarStage.Supernova;
            if (age >= 60f) return O_StarStage.RedGiant;
            if (age >= 30f) return O_StarStage.Orange;
            return O_StarStage.Yellow;
        }

        public static bool AllowsPlanets(O_StarStage stage) =>
            stage == O_StarStage.Yellow || stage == O_StarStage.Orange;

        public static double GetProductionMultiplier(O_StarStage stage, Core.O_GameBalance balance)
        {
            return stage switch
            {
                O_StarStage.Yellow => balance.yellowProductionMultiplier,
                O_StarStage.Orange => balance.orangeProductionMultiplier,
                O_StarStage.RedGiant => balance.redGiantProductionMultiplier,
                O_StarStage.Supernova => balance.supernovaProductionMultiplier,
                _ => 0
            };
        }
    }
}
