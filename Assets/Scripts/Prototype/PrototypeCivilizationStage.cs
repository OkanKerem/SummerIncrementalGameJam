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
        public static float GetDnaMultiplier(PrototypeCivilizationStage stage) =>
            stage switch
            {
                PrototypeCivilizationStage.Life => 0.2f,
                PrototypeCivilizationStage.PrimitiveLife => 0.35f,
                PrototypeCivilizationStage.Tribe => 0.55f,
                PrototypeCivilizationStage.Civilization => 0.85f,
                PrototypeCivilizationStage.IndustrialAge => 1.25f,
                PrototypeCivilizationStage.SpaceAge => 2f,
                _ => 0f
            };

        public static string GetLabel(PrototypeCivilizationStage stage) =>
            stage switch
            {
                PrototypeCivilizationStage.Life => "Life",
                PrototypeCivilizationStage.PrimitiveLife => "Primitive Life",
                PrototypeCivilizationStage.Tribe => "Tribe",
                PrototypeCivilizationStage.Civilization => "Civilization",
                PrototypeCivilizationStage.IndustrialAge => "Industrial Age",
                PrototypeCivilizationStage.SpaceAge => "Space Age",
                _ => "No Life"
            };

        public static bool CanProgress(PrototypeCivilizationStage stage) =>
            stage < PrototypeCivilizationStage.SpaceAge;

        public static PrototypeCivilizationStage Next(PrototypeCivilizationStage stage) =>
            (PrototypeCivilizationStage)((int)stage + 1);
    }
}
