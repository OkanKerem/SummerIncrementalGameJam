namespace Universes.Prototype
{
    public enum PrototypeCivilizationStage
    {
        NoLife,
        Life,
        PrimitiveLife,
        Tribe,
        Civilization,
        IndustrialAge,
        SpaceAge
    }

    public static class PrototypeCivilizationUtility
    {
        private static readonly PrototypeCivilizationBalanceConfig DefaultBalance = new();

        public static float GetDnaMultiplier(PrototypeCivilizationStage stage,
            PrototypeCivilizationBalanceConfig balance = null) =>
            (balance ?? DefaultBalance).GetStageDnaValue(stage);

        public static string GetLabel(PrototypeCivilizationStage stage,
            PrototypeCivilizationBalanceConfig balance = null) =>
            (balance ?? DefaultBalance).GetStageName(stage);

        public static float GetProgressRequirement(PrototypeCivilizationStage stage,
            PrototypeCivilizationBalanceConfig balance = null) =>
            (balance ?? DefaultBalance).GetStageProgressRequirement(stage);

        public static bool CanProgress(PrototypeCivilizationStage stage,
            PrototypeCivilizationBalanceConfig balance = null) =>
            (balance ?? DefaultBalance).CanProgress(stage);

        public static PrototypeCivilizationStage Next(PrototypeCivilizationStage stage) =>
            (PrototypeCivilizationStage)((int)stage + 1);
    }
}
