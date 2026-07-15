using UnityEngine;

namespace Universes.Game
{
    public enum PlanetType
    {
        Rocky,
        Ocean,
        Lava,
        Ice,
        GasGiant,
        Toxic,
        Crystal,
        Desert,
        Forest
    }

    public static class PlanetTypeUtility
    {
        public static string GetLabel(PlanetType type) =>
            type switch
            {
                PlanetType.GasGiant => "Gas Giant",
                _ => type.ToString()
            };

        public static string GetLabel(PlanetTypeDefinition definition) =>
            definition != null ? definition.GetDisplayName() : "Planet";

        public static PlanetTypeDefinition CreateFallbackDefinition(PlanetType type,
            PlanetBalanceConfig balance = null)
        {
            var def = ScriptableObject.CreateInstance<PlanetTypeDefinition>();
            def.planetType = type;
            def.displayName = GetLabel(type);
            ApplyFallbackStats(def, type, balance);
            return def;
        }

        private static void ApplyFallbackStats(PlanetTypeDefinition def, PlanetType type,
            PlanetBalanceConfig balance)
        {
            var config = balance?.GetPlanetTypeConfig(type);
            if (config != null)
            {
                def.baseClickValue = config.baseClickReward;
                def.habitability = config.habitabilityValue;
                def.dnaChance = config.dnaChance;
                def.maxDurability = config.baseDurability;
                def.canCivilize = config.canCivilize;
                def.spawnWeight = config.planetTypeWeight;
            }

            switch (type)
            {
                case PlanetType.Rocky:
                    def.planetColor = new Color(0.65f, 0.55f, 0.45f);
                    break;
                case PlanetType.Ocean:
                    def.planetColor = new Color(0.25f, 0.55f, 0.95f);
                    break;
                case PlanetType.Lava:
                    def.planetColor = new Color(0.95f, 0.35f, 0.15f);
                    break;
                case PlanetType.Ice:
                    def.planetColor = new Color(0.75f, 0.9f, 1f);
                    break;
                case PlanetType.GasGiant:
                    def.planetColor = new Color(0.85f, 0.65f, 0.4f);
                    break;
                case PlanetType.Toxic:
                    def.planetColor = new Color(0.55f, 0.85f, 0.25f);
                    break;
                case PlanetType.Crystal:
                    def.planetColor = new Color(0.75f, 0.45f, 0.95f);
                    break;
                case PlanetType.Desert:
                    def.planetColor = new Color(0.9f, 0.75f, 0.35f);
                    break;
                case PlanetType.Forest:
                    def.planetColor = new Color(0.25f, 0.75f, 0.35f);
                    break;
            }
        }

        public static PlanetType RollRandomFallback(PlanetBalanceConfig balance = null) =>
            balance != null
                ? balance.RollFallbackPlanetType()
                : PlanetType.Rocky;
    }
}
