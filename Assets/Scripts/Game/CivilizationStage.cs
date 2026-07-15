namespace Universes.Game
{
    public enum CivilizationStage
    {
        NoLife,
        PrimitiveLife,
        CivilizationPhase,
        SpacePhase,
        HardSpace
    }

    public static class CivilizationUtility
    {
        private static readonly CivilizationBalanceConfig DefaultBalance = new();

        public static float GetDnaMultiplier(CivilizationStage stage,
            CivilizationBalanceConfig balance = null) =>
            (balance ?? DefaultBalance).GetStageDnaValue(stage);

        public static string GetLabel(CivilizationStage stage,
            CivilizationBalanceConfig balance = null) =>
            (balance ?? DefaultBalance).GetStageName(stage);

        public static float GetProgressRequirement(CivilizationStage stage,
            CivilizationBalanceConfig balance = null) =>
            (balance ?? DefaultBalance).GetStageProgressRequirement(stage);

        public static bool CanProgress(CivilizationStage stage,
            CivilizationBalanceConfig balance = null) =>
            (balance ?? DefaultBalance).CanProgress(stage);

        public static CivilizationStage Next(CivilizationStage stage) =>
            (CivilizationStage)((int)stage + 1);
    }
}
