namespace Universes.Prototype
{
    public enum PrototypeGameplayMode
    {
        SingleStarSystemAge,
        MultiStarSystemAge,
        FullCosmic
    }

    public static class PrototypeGameplayFeatures
    {
        public static bool IsSingleStarMode(PrototypeGameplayMode mode) =>
            mode == PrototypeGameplayMode.SingleStarSystemAge;

        public static bool UsesEntropy(PrototypeGameplayMode mode) =>
            mode == PrototypeGameplayMode.MultiStarSystemAge || mode == PrototypeGameplayMode.FullCosmic;

        public static bool UsesMultiStar(PrototypeGameplayMode mode) =>
            mode == PrototypeGameplayMode.MultiStarSystemAge || mode == PrototypeGameplayMode.FullCosmic;

        public static bool UsesBlackHoles(PrototypeGameplayMode mode) =>
            mode == PrototypeGameplayMode.FullCosmic;

        public static bool UsesParticleVacuum(PrototypeGameplayMode mode) =>
            mode == PrototypeGameplayMode.FullCosmic;

        public static bool UsesUniverseCollapse(PrototypeGameplayMode mode) =>
            mode == PrototypeGameplayMode.FullCosmic;

        public const bool Step2ExpansionAvailable = true;
    }
}
