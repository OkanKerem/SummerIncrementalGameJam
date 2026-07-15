namespace Universes.Game
{
    public class RunStats
    {
        public double TotalStardustProduced { get; private set; }
        public int TotalStarClicks { get; private set; }
        public int TotalPlanetClicks { get; private set; }
        public int StarsCreated { get; private set; }
        public int PlanetsCreated { get; private set; }
        public int PlanetsDestroyed { get; private set; }
        public int HighestPlanetCount { get; private set; }
        public int LifePlanetsReached { get; private set; }
        public string HighestCivilizationLabel { get; private set; } = "No Life";
        public int SupernovaCount { get; private set; }
        public int StarCollisionCount { get; private set; }
        public int BlackHolesCreated { get; private set; }
        public int DnaFragmentsCollected { get; private set; }
        public float BlackHoleDnaPotential { get; private set; }
        public float SurvivalTimeSeconds { get; set; }
        public float FinalEntropy { get; private set; }

        public void Reset()
        {
            TotalStardustProduced = 0;
            TotalStarClicks = 0;
            TotalPlanetClicks = 0;
            StarsCreated = 0;
            PlanetsCreated = 0;
            PlanetsDestroyed = 0;
            HighestPlanetCount = 0;
            LifePlanetsReached = 0;
            HighestCivilizationLabel = "No Life";
            SupernovaCount = 0;
            StarCollisionCount = 0;
            BlackHolesCreated = 0;
            DnaFragmentsCollected = 0;
            BlackHoleDnaPotential = 0f;
            SurvivalTimeSeconds = 0f;
            FinalEntropy = 0f;
        }

        public void RecordStardustProduced(double amount)
        {
            if (amount > 0)
                TotalStardustProduced += amount;
        }

        public void RecordClick() => TotalStarClicks++;
        public void RecordPlanetClick() => TotalPlanetClicks++;
        public void RecordStarCreated() => StarsCreated++;

        public void RecordPlanetCreated(int currentCount)
        {
            PlanetsCreated++;
            if (currentCount > HighestPlanetCount)
                HighestPlanetCount = currentCount;
        }

        public void RecordPlanetDestroyed() => PlanetsDestroyed++;
        public void RecordLifePlanet() => LifePlanetsReached++;

        public void RecordHighestCivilization(CivilizationStage stage)
        {
            HighestCivilizationLabel = CivilizationUtility.GetLabel(stage);
        }

        public void RecordSupernova() => SupernovaCount++;
        public void RecordCollision() => StarCollisionCount++;
        public void RecordBlackHole() => BlackHolesCreated++;
        public void RecordDnaFragment() => DnaFragmentsCollected++;
        public void AddBlackHoleDnaPotential(float amount) => BlackHoleDnaPotential += amount;
        public void SetFinalEntropy(float entropy) => FinalEntropy = entropy;
    }
}
