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

        // Stardust is credited directly in all modes; floating particles are DNA-only.
        public static bool UsesParticleVacuum(GameplayMode mode) => false;

        public static bool UsesUniverseCollapse(GameplayMode mode) =>
            mode == GameplayMode.FullCosmic;

        public const bool Step2ExpansionAvailable = true;
        public const bool Step3ExpansionAvailable = true;
    }
}
