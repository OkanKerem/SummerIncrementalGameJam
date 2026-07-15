using Universes.Core;
using Universes.Prestige;
using UnityEngine;

namespace Universes.Core
{
    public static class O_DefaultGameData
    {
        public static O_GameBalance CreateBalance()
        {
            var b = ScriptableObject.CreateInstance<O_GameBalance>();
            return b;
        }

        public static O_UpgradeCatalog CreateUpgradeCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<O_UpgradeCatalog>();
            catalog.upgrades = new[]
            {
                MakeUpgrade("big_bang", "Stronger Big Bang", "+1 starting star per level", O_UpgradeEffectType.StartingStars, 1, 15),
                MakeUpgrade("cosmic_reserves", "Cosmic Reserves", "+10 starting stardust per level", O_UpgradeEffectType.StartingStardust, 10, 12),
                MakeUpgrade("entropy_dampening", "Entropy Dampening", "-10% entropy rate per level", O_UpgradeEffectType.EntropyReduction, 0.1, 20),
                MakeUpgrade("stellar_longevity", "Stellar Longevity", "+15% star lifespan per level", O_UpgradeEffectType.StarLifespan, 0.15, 18),
                MakeUpgrade("stellar_harvest", "Stellar Harvest", "+20% stardust production per level", O_UpgradeEffectType.StardustProduction, 0.2, 15),
                MakeUpgrade("click_resonance", "Click Resonance", "+25% click yield per level", O_UpgradeEffectType.ClickYield, 0.25, 12),
                MakeUpgrade("planet_forge", "Planet Forge", "-10% planet cost per level", O_UpgradeEffectType.PlanetCostReduction, 0.1, 14),
                MakeUpgrade("spark_of_life", "Spark of Life", "+30% life chance per level", O_UpgradeEffectType.LifeChance, 0.3, 22),
                MakeUpgrade("genetic_memory", "Genetic Memory", "+15% DNA from collapse per level", O_UpgradeEffectType.DnaMultiplier, 0.15, 25),
                MakeUpgrade("parallel_echo", "Parallel Echo", "+0.5 passive stardust/sec between runs", O_UpgradeEffectType.ParallelEcho, 0.5, 30)
            };
            return catalog;
        }

        private static O_UpgradeDefinition MakeUpgrade(string id, string name, string desc, O_UpgradeEffectType type, double effect, double cost)
        {
            var def = ScriptableObject.CreateInstance<O_UpgradeDefinition>();
            def.id = id;
            def.displayName = name;
            def.description = desc;
            def.effectType = type;
            def.effectPerLevel = effect;
            def.baseCost = cost;
            def.maxLevel = 5;
            return def;
        }

        public static O_VariantCatalog CreateVariantCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<O_VariantCatalog>();
            catalog.variants = new[]
            {
                MakeVariant("volatile", "Volatile Universe", "+30% Stardust production", "+25% Entropy growth", 1.3, 1.25, 1, 1, 1),
                MakeVariant("stable", "Stable Universe", "-25% Entropy growth", "-20% Stardust production", 0.8, 0.75, 1, 1, 1),
                MakeVariant("life_rich", "Life-Rich Universe", "+50% life development chance", "-20% star lifespan", 1, 1, 1.5, 0.8, 1)
            };
            return catalog;
        }

        private static O_ParallelVariant MakeVariant(string id, string name, string bonus, string drawback,
            double stardust, double entropy, double life, double lifespan, double dna)
        {
            var v = ScriptableObject.CreateInstance<O_ParallelVariant>();
            v.id = id;
            v.displayName = name;
            v.bonusDescription = bonus;
            v.drawbackDescription = drawback;
            v.stardustMultiplier = stardust;
            v.entropyMultiplier = entropy;
            v.lifeChanceMultiplier = life;
            v.starLifespanMultiplier = lifespan;
            v.dnaMultiplier = dna;
            return v;
        }

        public static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = size / 2f;
            var radius = size / 2f - 1;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
