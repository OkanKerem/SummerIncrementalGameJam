using Universes.Core;

namespace Universes.Prestige
{
    public class O_RunModifiers
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

        public static O_RunModifiers From(O_PrestigeState prestige, O_UpgradeCatalog catalog, O_VariantCatalog variants, O_GameBalance balance)
        {
            var mods = new O_RunModifiers();

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
                        case O_UpgradeEffectType.StartingStars:
                            mods.BonusStartingStars += (int)total;
                            break;
                        case O_UpgradeEffectType.StartingStardust:
                            mods.BonusStartingStardust += total;
                            break;
                        case O_UpgradeEffectType.EntropyReduction:
                            mods.EntropyReductionMultiplier += total;
                            break;
                        case O_UpgradeEffectType.StarLifespan:
                            mods.StarLifespanMultiplier += total;
                            break;
                        case O_UpgradeEffectType.StardustProduction:
                            mods.StardustProductionMultiplier += total;
                            break;
                        case O_UpgradeEffectType.ClickYield:
                            mods.ClickYieldMultiplier += total;
                            break;
                        case O_UpgradeEffectType.ClickAgeReduction:
                            mods.ClickAgeReduction += total;
                            break;
                        case O_UpgradeEffectType.PlanetCostReduction:
                            mods.PlanetCostReduction += total;
                            break;
                        case O_UpgradeEffectType.MaxPlanets:
                            mods.BonusMaxPlanets += (int)total;
                            break;
                        case O_UpgradeEffectType.LifeChance:
                            mods.LifeChanceMultiplier += total;
                            break;
                        case O_UpgradeEffectType.DnaMultiplier:
                            mods.DnaMultiplier += total;
                            break;
                        case O_UpgradeEffectType.ParallelEcho:
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

        public int GetMaxPlanetsPerStar(O_GameBalance balance) =>
            balance.maxPlanetsPerStar + BonusMaxPlanets;
    }
}
