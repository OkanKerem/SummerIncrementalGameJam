namespace Universes.Core
{
    public static class O_CosmicNameGenerator
    {
        private static readonly string[] StarPrefixes =
        {
            "Astra",
            "Sol",
            "Vega",
            "Nova",
            "Helio",
            "Cinder"
        };

        private static readonly string[] StarSuffixes =
        {
            "flare",
            "heart",
            "spire",
            "crown",
            "pulse",
            "wake"
        };

        private static readonly string[] PlanetPrefixes =
        {
            "Eden",
            "Kora",
            "Mira",
            "Orion",
            "Vale",
            "Thalos"
        };

        private static readonly string[] PlanetSuffixes =
        {
            "reach",
            "fall",
            "mere",
            "hollow",
            "rise",
            "deep"
        };

        public static string GenerateStarName(int starId) =>
            $"{Pick(StarPrefixes, starId)}{Pick(StarSuffixes, starId * 7)}-{starId:00}";

        public static string GeneratePlanetName(int planetId) =>
            $"{Pick(PlanetPrefixes, planetId)}{Pick(PlanetSuffixes, planetId * 7)}-{planetId:00}";

        private static string Pick(string[] values, int seed)
        {
            if (values == null || values.Length == 0)
                return string.Empty;

            return values[System.Math.Abs(seed) % values.Length];
        }
    }
}
