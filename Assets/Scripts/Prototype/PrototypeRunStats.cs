namespace Universes.Prototype
{
    public class PrototypeRunStats
    {
        public double TotalStardustProduced { get; private set; }
        public int TotalStarClicks { get; private set; }
        public int StarsCreated { get; private set; }
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
            StarsCreated = 0;
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
        public void RecordStarCreated() => StarsCreated++;
        public void RecordSupernova() => SupernovaCount++;
        public void RecordCollision() => StarCollisionCount++;
        public void RecordBlackHole() => BlackHolesCreated++;
        public void RecordDnaFragment() => DnaFragmentsCollected++;
        public void AddBlackHoleDnaPotential(float amount) => BlackHoleDnaPotential += amount;
        public void SetFinalEntropy(float entropy) => FinalEntropy = entropy;
    }
}
