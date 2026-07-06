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

        public static PrototypePlanetTypeDefinition CreateFallbackDefinition(PrototypePlanetType type)
        {
            var def = ScriptableObject.CreateInstance<PrototypePlanetTypeDefinition>();
            def.planetType = type;
            def.displayName = GetLabel(type);
            ApplyFallbackStats(def, type);
            return def;
        }

        private static void ApplyFallbackStats(PrototypePlanetTypeDefinition def, PrototypePlanetType type)
        {
            switch (type)
            {
                case PrototypePlanetType.Rocky:
                    def.baseClickValue = 3;
                    def.habitability = 0.55f;
                    def.dnaChance = 0.12f;
                    def.maxDurability = 100f;
                    def.canCivilize = true;
                    def.spawnWeight = 18f;
                    def.planetColor = new Color(0.65f, 0.55f, 0.45f);
                    break;
                case PrototypePlanetType.Ocean:
                    def.baseClickValue = 2;
                    def.habitability = 0.85f;
                    def.dnaChance = 0.15f;
                    def.maxDurability = 90f;
                    def.canCivilize = true;
                    def.spawnWeight = 14f;
                    def.planetColor = new Color(0.25f, 0.55f, 0.95f);
                    break;
                case PrototypePlanetType.Lava:
                    def.baseClickValue = 6;
                    def.habitability = 0.15f;
                    def.dnaChance = 0.08f;
                    def.maxDurability = 70f;
                    def.canCivilize = false;
                    def.spawnWeight = 12f;
                    def.planetColor = new Color(0.95f, 0.35f, 0.15f);
                    break;
                case PrototypePlanetType.Ice:
                    def.baseClickValue = 2;
                    def.habitability = 0.35f;
                    def.dnaChance = 0.1f;
                    def.maxDurability = 120f;
                    def.canCivilize = true;
                    def.spawnWeight = 12f;
                    def.planetColor = new Color(0.75f, 0.9f, 1f);
                    break;
                case PrototypePlanetType.GasGiant:
                    def.baseClickValue = 7;
                    def.habitability = 0f;
                    def.dnaChance = 0.05f;
                    def.maxDurability = 110f;
                    def.canCivilize = false;
                    def.spawnWeight = 10f;
                    def.planetColor = new Color(0.85f, 0.65f, 0.4f);
                    break;
                case PrototypePlanetType.Toxic:
                    def.baseClickValue = 4;
                    def.habitability = 0.2f;
                    def.dnaChance = 0.28f;
                    def.maxDurability = 80f;
                    def.canCivilize = true;
                    def.spawnWeight = 8f;
                    def.planetColor = new Color(0.55f, 0.85f, 0.25f);
                    break;
                case PrototypePlanetType.Crystal:
                    def.baseClickValue = 8;
                    def.habitability = 0.4f;
                    def.dnaChance = 0.35f;
                    def.maxDurability = 55f;
                    def.canCivilize = true;
                    def.spawnWeight = 4f;
                    def.planetColor = new Color(0.75f, 0.45f, 0.95f);
                    break;
                case PrototypePlanetType.Desert:
                    def.baseClickValue = 3;
                    def.habitability = 0.45f;
                    def.dnaChance = 0.1f;
                    def.maxDurability = 95f;
                    def.canCivilize = true;
                    def.spawnWeight = 12f;
                    def.planetColor = new Color(0.9f, 0.75f, 0.35f);
                    break;
                case PrototypePlanetType.Forest:
                    def.baseClickValue = 3;
                    def.habitability = 0.9f;
                    def.dnaChance = 0.18f;
                    def.maxDurability = 85f;
                    def.canCivilize = true;
                    def.spawnWeight = 10f;
                    def.planetColor = new Color(0.25f, 0.75f, 0.35f);
                    break;
            }
        }

        public static PrototypePlanetType RollRandomFallback()
        {
            var values = (PrototypePlanetType[])System.Enum.GetValues(typeof(PrototypePlanetType));
            return values[Random.Range(0, values.Length)];
        }
    }
}
