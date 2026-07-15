namespace Universes.Game
{
    public enum GameplayMode
    {
        SingleStarSystemAge,
        MultiStarSystemAge,
        FullCosmic
    }

    public static class GameplayFeatures
    {
        public static bool IsSingleStarMode(GameplayMode mode) =>
            mode == GameplayMode.SingleStarSystemAge;

        public static bool UsesEntropy(GameplayMode mode) =>
            mode == GameplayMode.MultiStarSystemAge || mode == GameplayMode.FullCosmic;

        public static bool UsesMultiStar(GameplayMode mode) =>
            mode == GameplayMode.MultiStarSystemAge || mode == GameplayMode.FullCosmic;

        public static bool UsesBlackHoles(GameplayMode mode) =>
            mode == GameplayMode.FullCosmic;

        public static bool UsesParticleVacuum(GameplayMode mode) =>
            mode == GameplayMode.FullCosmic;

        public static bool UsesUniverseCollapse(GameplayMode mode) =>
            mode == GameplayMode.FullCosmic;

        public const bool Step2ExpansionAvailable = true;
    }
}
