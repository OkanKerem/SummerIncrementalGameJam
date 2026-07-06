namespace Universes.Prototype
{
    public static class PrototypePlanetBalance
    {
        public const double CreatePlanetCost = 30;
        public const int BaseMaxPlanets = 2;
        public const float BaseOrbitRadius = 1.4f;
        public const float OrbitRadiusStep = 0.55f;
        public const float OrbitSpeed = 28f;

        public const float AutoFormationCheckInterval = 3f;
        public const float BaseAutoFormationChance = 0.01f;
        public const float AutoFormationChancePerLevel = 0.008f;

        public const float BasePlanetDnaChance = 0.02f;
        public const float PlanetDnaChancePerLevel = 0.012f;

        public const float BaseHabitableRollBonus = 0.05f;
        public const float HabitableChancePerLevel = 0.06f;

        public const float BasePlanetClickDamage = 8f;
        public const float PlanetClickValuePerLevel = 1.5f;

        public const float CivilizationTickInterval = 2.5f;
        public const float LifeProgressBase = 0.08f;
        public const float DnaTickInterval = 1f;
    }
}
