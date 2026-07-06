using UnityEngine;

namespace Universes.Prototype
{
    public enum PrototypePlanetType
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

    public static class PrototypePlanetTypeUtility
    {
        public static string GetLabel(PrototypePlanetType type) =>
            type switch
            {
                PrototypePlanetType.GasGiant => "Gas Giant",
                _ => type.ToString()
            };

        public static string GetLabel(PrototypePlanetTypeDefinition definition) =>
            definition != null ? definition.GetDisplayName() : "Planet";

        public static PrototypePlanetTypeDefinition CreateFallbackDefinition(PrototypePlanetType type,
            PrototypePlanetBalanceConfig balance = null)
        {
            var def = ScriptableObject.CreateInstance<PrototypePlanetTypeDefinition>();
            def.planetType = type;
            def.displayName = GetLabel(type);
            ApplyFallbackStats(def, type, balance);
            return def;
        }

        private static void ApplyFallbackStats(PrototypePlanetTypeDefinition def, PrototypePlanetType type,
            PrototypePlanetBalanceConfig balance)
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
                case PrototypePlanetType.Rocky:
                    def.planetColor = new Color(0.65f, 0.55f, 0.45f);
                    break;
                case PrototypePlanetType.Ocean:
                    def.planetColor = new Color(0.25f, 0.55f, 0.95f);
                    break;
                case PrototypePlanetType.Lava:
                    def.planetColor = new Color(0.95f, 0.35f, 0.15f);
                    break;
                case PrototypePlanetType.Ice:
                    def.planetColor = new Color(0.75f, 0.9f, 1f);
                    break;
                case PrototypePlanetType.GasGiant:
                    def.planetColor = new Color(0.85f, 0.65f, 0.4f);
                    break;
                case PrototypePlanetType.Toxic:
                    def.planetColor = new Color(0.55f, 0.85f, 0.25f);
                    break;
                case PrototypePlanetType.Crystal:
                    def.planetColor = new Color(0.75f, 0.45f, 0.95f);
                    break;
                case PrototypePlanetType.Desert:
                    def.planetColor = new Color(0.9f, 0.75f, 0.35f);
                    break;
                case PrototypePlanetType.Forest:
                    def.planetColor = new Color(0.25f, 0.75f, 0.35f);
                    break;
            }
        }

        public static PrototypePlanetType RollRandomFallback(PrototypePlanetBalanceConfig balance = null) =>
            balance != null
                ? balance.RollFallbackPlanetType()
                : PrototypePlanetType.Rocky;
    }
}
