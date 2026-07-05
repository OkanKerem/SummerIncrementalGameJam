using Universes.Core;

namespace Universes.Prestige
{
    public class RunModifiers
    {
        public int BonusStartingStars;
        public double BonusStartingStardust;
        public double EntropyReductionMultiplier = 1;
        public double StarLifespanMultiplier = 1;
        public double StardustProductionMultiplier = 1;
        public double ClickYieldMultiplier = 1;
        public double ClickAgeReduction;
        public double PlanetCostReduction;
        public int BonusMaxPlanets;
        public double LifeChanceMultiplier = 1;
        public double DnaMultiplier = 1;
        public double ParallelEchoPerSecond;

        public double VariantStardustMultiplier = 1;
        public double VariantEntropyMultiplier = 1;
        public double VariantLifeChanceMultiplier = 1;
        public double VariantStarLifespanMultiplier = 1;
        public double VariantDnaMultiplier = 1;

        public static RunModifiers From(PrestigeState prestige, UpgradeCatalog catalog, VariantCatalog variants, GameBalance balance)
        {
            var mods = new RunModifiers();

            if (catalog?.upgrades != null)
            {
                foreach (var def in catalog.upgrades)
                {
                    if (def == null)
                        continue;

                    var level = prestige.GetUpgradeLevel(def.id);
                    if (level <= 0)
                        continue;

                    var total = def.effectPerLevel * level;
                    switch (def.effectType)
                    {
                        case UpgradeEffectType.StartingStars:
                            mods.BonusStartingStars += (int)total;
                            break;
                        case UpgradeEffectType.StartingStardust:
                            mods.BonusStartingStardust += total;
                            break;
                        case UpgradeEffectType.EntropyReduction:
                            mods.EntropyReductionMultiplier += total;
                            break;
                        case UpgradeEffectType.StarLifespan:
                            mods.StarLifespanMultiplier += total;
                            break;
                        case UpgradeEffectType.StardustProduction:
                            mods.StardustProductionMultiplier += total;
                            break;
                        case UpgradeEffectType.ClickYield:
                            mods.ClickYieldMultiplier += total;
                            break;
                        case UpgradeEffectType.ClickAgeReduction:
                            mods.ClickAgeReduction += total;
                            break;
                        case UpgradeEffectType.PlanetCostReduction:
                            mods.PlanetCostReduction += total;
                            break;
                        case UpgradeEffectType.MaxPlanets:
                            mods.BonusMaxPlanets += (int)total;
                            break;
                        case UpgradeEffectType.LifeChance:
                            mods.LifeChanceMultiplier += total;
                            break;
                        case UpgradeEffectType.DnaMultiplier:
                            mods.DnaMultiplier += total;
                            break;
                        case UpgradeEffectType.ParallelEcho:
                            mods.ParallelEchoPerSecond += total;
                            break;
                    }
                }
            }

            var variant = variants?.GetById(prestige.ActiveVariantId);
            if (variant != null)
            {
                mods.VariantStardustMultiplier = variant.stardustMultiplier;
                mods.VariantEntropyMultiplier = variant.entropyMultiplier;
                mods.VariantLifeChanceMultiplier = variant.lifeChanceMultiplier;
                mods.VariantStarLifespanMultiplier = variant.starLifespanMultiplier;
                mods.VariantDnaMultiplier = variant.dnaMultiplier;
            }

            return mods;
        }

        public double GetEffectiveAgeRate(double baseRate) =>
            baseRate / (StarLifespanMultiplier * VariantStarLifespanMultiplier);

        public double GetEffectiveProductionMultiplier() =>
            StardustProductionMultiplier * VariantStardustMultiplier;

        public double GetEffectiveEntropyBase(double baseRate) =>
            baseRate * VariantEntropyMultiplier / EntropyReductionMultiplier;

        public int GetMaxPlanetsPerStar(GameBalance balance) =>
            balance.maxPlanetsPerStar + BonusMaxPlanets;
    }
}
