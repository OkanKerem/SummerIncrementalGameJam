using System;
using UnityEngine;

namespace Universes.Game
{
    [Serializable]
    public class RunSaveData
    {
        public double stardust;
        public double dnaPotential;
        public int dnaFragments;
        public float entropy;
        public int gameplayMode;
        public float survivalTimeSeconds;
        public int clickPowerLevel;
        public int clickPowerPercentLevel;
        public int passiveProductionLevel;
        public int starStabilityLevel;
        public int supernovaBonusLevel;
        public int clickCollectRadiusLevel;
        public int maxPlanetCountLevel;
        public int autoPlanetFormationLevel;
        public int planetDnaChanceLevel;
        public int planetClickValueLevel;
        public int habitablePlanetChanceLevel;
        public int universeExpandedLevel;
        public int cosmicExpandedLevel;
        public int maxStarCountLevel;
        public int advancedStarStabilityLevel;
        public int planetPassiveProductionLevel;
        public int starPlanetClickValueLevel;
        public int starPassiveProductionPercentLevel;
        public int collisionDnaProductionLevel;
        public int entropyReductionLevel;
        public int speciesDnaProductionLevel;
        public int collisionAttractionLevel;
        public int blackHoleStabilizationLevel;
        public int blackHoleMemoryLevel;
        public int spaceAgeDnaLevel;
        public int spaceAgeProgressionLevel;
        public int cosmicEventDnaLevel;
        public int entropyEqualizationLevel;
        public int orbitalDnaLevel;
        public int autoStarFormationLevel;
    }

    public static class RunSave
    {
        private const string SaveKey = "Universes_RunSave_v1";
        private const string NewGameFlagKey = "Universes_ForceNewGame";
        private const string ResetPrestigeFlagKey = "Universes_ResetPrestigeOnNewGame";

        public static bool HasSave => PlayerPrefs.HasKey(SaveKey);

        public static bool IsNewGamePending => PlayerPrefs.GetInt(NewGameFlagKey, 0) == 1;

        public static bool ConsumeNewGameRequest()
        {
            if (PlayerPrefs.GetInt(NewGameFlagKey, 0) != 1)
                return false;

            PlayerPrefs.DeleteKey(NewGameFlagKey);
            PlayerPrefs.Save();
            return true;
        }

        public static bool ConsumeResetPrestigeRequest()
        {
            if (PlayerPrefs.GetInt(ResetPrestigeFlagKey, 0) != 1)
                return false;

            PlayerPrefs.DeleteKey(ResetPrestigeFlagKey);
            PlayerPrefs.Save();
            return true;
        }

        public static void RequestNewGame(bool resetPrestige = false)
        {
            Clear();
            PlayerPrefs.SetInt(NewGameFlagKey, 1);
            if (resetPrestige)
                PlayerPrefs.SetInt(ResetPrestigeFlagKey, 1);
            PlayerPrefs.Save();
        }

        public static void Save(GameController controller)
        {
            if (controller == null || controller.IsRunEnded || IsNewGamePending)
                return;

            var upgrades = controller.Upgrades;
            var data = new RunSaveData
            {
                stardust = controller.Stardust,
                dnaPotential = controller.DnaPotential,
                dnaFragments = controller.DnaFragments,
                entropy = controller.Entropy,
                gameplayMode = (int)controller.GameplayMode,
                survivalTimeSeconds = controller.RunStats.SurvivalTimeSeconds,
                clickPowerLevel = upgrades.ClickPowerLevel,
                clickPowerPercentLevel = upgrades.ClickPowerPercentLevel,
                passiveProductionLevel = upgrades.PassiveProductionLevel,
                starStabilityLevel = upgrades.StarStabilityLevel,
                supernovaBonusLevel = upgrades.SupernovaBonusLevel,
                clickCollectRadiusLevel = upgrades.ClickCollectRadiusLevel,
                maxPlanetCountLevel = upgrades.MaxPlanetCountLevel,
                autoPlanetFormationLevel = upgrades.AutoPlanetFormationLevel,
                planetDnaChanceLevel = upgrades.PlanetDnaChanceLevel,
                planetClickValueLevel = upgrades.PlanetClickValueLevel,
                habitablePlanetChanceLevel = upgrades.HabitablePlanetChanceLevel,
                universeExpandedLevel = upgrades.UniverseExpandedLevel,
                cosmicExpandedLevel = upgrades.CosmicExpandedLevel,
                maxStarCountLevel = upgrades.MaxStarCountLevel,
                advancedStarStabilityLevel = upgrades.AdvancedStarStabilityLevel,
                planetPassiveProductionLevel = upgrades.PlanetPassiveProductionLevel,
                starPlanetClickValueLevel = upgrades.StarPlanetClickValueLevel,
                starPassiveProductionPercentLevel = upgrades.StarPassiveProductionPercentLevel,
                collisionDnaProductionLevel = upgrades.CollisionDnaProductionLevel,
                entropyReductionLevel = upgrades.EntropyReductionLevel,
                speciesDnaProductionLevel = upgrades.SpeciesDnaProductionLevel,
                collisionAttractionLevel = upgrades.CollisionAttractionLevel,
                blackHoleStabilizationLevel = upgrades.BlackHoleStabilizationLevel,
                blackHoleMemoryLevel = upgrades.BlackHoleMemoryLevel,
                spaceAgeDnaLevel = upgrades.SpaceAgeDnaLevel,
                spaceAgeProgressionLevel = upgrades.SpaceAgeProgressionLevel,
                cosmicEventDnaLevel = upgrades.CosmicEventDnaLevel,
                entropyEqualizationLevel = upgrades.EntropyEqualizationLevel,
                orbitalDnaLevel = upgrades.OrbitalDnaLevel,
                autoStarFormationLevel = upgrades.AutoStarFormationLevel
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            GameSession.MarkGameStarted(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            PlayerPrefs.Save();
        }

        public static bool TryLoad(out RunSaveData data)
        {
            data = null;
            if (!HasSave)
                return false;

            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            data = JsonUtility.FromJson<RunSaveData>(json);
            return data != null;
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }
}
