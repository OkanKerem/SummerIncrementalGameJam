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
        public static PrototypeStarStage FromAge(int age)
        {
            if (age >= 100) return PrototypeStarStage.Supernova;
            if (age >= 67) return PrototypeStarStage.RedGiant;
            if (age >= 34) return PrototypeStarStage.Orange;
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
