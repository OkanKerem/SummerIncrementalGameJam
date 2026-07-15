using UnityEngine;

namespace Universes.Game
{
    public static class SpeciesNaming
    {
        private static readonly string[] PlanetPrefixes =
        {
            "Ela", "Vora", "Nyx", "Auri", "Thal", "Ixo", "Mira", "Sera", "Kora", "Zei"
        };

        private static readonly string[] PlanetSuffixes =
        {
            "rion", "mere", "thos", "vara", "lia", "dor", "nox", "aris", "uun", "ora"
        };

        private static readonly string[] SpeciesPrefixes =
        {
            "Vel", "Ari", "Mor", "Syl", "Nim", "Ora", "Kai", "Zen", "Iri", "Tal"
        };

        private static readonly string[] SpeciesSuffixes =
        {
            "ari", "ans", "iri", "ites", "ori", "ae", "ariq", "ell", "uun", "esh"
        };

        private static readonly string[] CivilizationForms =
        {
            "Concord", "Assembly", "Accord", "Collective", "Union", "Compact"
        };

        private static readonly string[] StarPrefixes =
        {
            "Sol", "Aster", "Helio", "Cael", "Vey", "Oris", "Luma", "Siri", "Nara", "Eos"
        };

        private static readonly string[] StarSuffixes =
        {
            "ion", "ara", "eth", "os", "iel", "or", "is", "une", "ar", "ix"
        };

        private static readonly string[] FlavorTemplates =
        {
            "The {0} first emerged beneath the changing skies of {1}.",
            "The {0} adapted to the strange rhythms of {1}.",
            "The {0} learned to read the old patterns of {1}.",
            "The {0} grew quietly across the living regions of {1}."
        };

        public static string GeneratePlanetName(int planetId) =>
            $"{Pick(PlanetPrefixes)}{Pick(PlanetSuffixes)}-{planetId:00}";

        public static string GenerateStarName(int starId) =>
            $"{Pick(StarPrefixes)}{Pick(StarSuffixes)}-{starId:00}";

        public static string GenerateSpeciesName() =>
            $"{Pick(SpeciesPrefixes)}{Pick(SpeciesSuffixes)}";

        public static string GenerateCivilizationName(string speciesName) =>
            $"The {speciesName} {Pick(CivilizationForms)}";

        public static string GenerateFlavor(string planetName, string speciesName) =>
            string.Format(Pick(FlavorTemplates), speciesName, planetName);

        private static string Pick(string[] values)
        {
            if (values == null || values.Length == 0)
                return string.Empty;

            return values[Random.Range(0, values.Length)];
        }
    }
}
