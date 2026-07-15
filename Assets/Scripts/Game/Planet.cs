namespace Universes.Game
{
    public class Planet
    {
        public int Id { get; }
        public int HostStarId { get; }
        public PlanetTypeDefinition Definition { get; }
        public PlanetType Type => Definition != null ? Definition.planetType : PlanetType.Rocky;
        public int OrbitSlot { get; }
        public float OrbitRadius { get; }
        public float OrbitAngle { get; set; }
        public float Durability { get; private set; }
        public float MaxDurability { get; }
        public bool IsHabitable { get; }
        public CivilizationStage CivilizationStage { get; private set; }
        public float CivilizationProgress { get; private set; }
        public string PlanetName { get; private set; }
        public string SpeciesName { get; private set; }
        public string SpeciesDescription { get; private set; }
        public string CivilizationName { get; private set; }
        public int Intelligence { get; private set; }
        public int Aggression { get; private set; }

        public bool IsAlive => Durability > 0f;
        public bool HasLife => CivilizationStage > CivilizationStage.NoLife;
        public bool HasSpecies => !string.IsNullOrWhiteSpace(SpeciesName);
        public bool LifeCountedForStats { get; set; }

        public Planet(int id, int hostStarId, PlanetTypeDefinition definition, int orbitSlot,
            float orbitRadius, float orbitAngle, bool isHabitable, float fallbackMaxDurability,
            float durabilityMultiplier = 1f)
        {
            Id = id;
            HostStarId = hostStarId;
            Definition = definition;
            OrbitSlot = orbitSlot;
            OrbitRadius = orbitRadius;
            OrbitAngle = orbitAngle;
            IsHabitable = isHabitable;

            var baseDurability = definition != null ? definition.maxDurability : fallbackMaxDurability;
            MaxDurability = UnityEngine.Mathf.Max(
                1f,
                baseDurability * UnityEngine.Mathf.Max(0f, durabilityMultiplier));
            Durability = MaxDurability;
            CivilizationStage = CivilizationStage.NoLife;
        }

        public void Damage(float amount)
        {
            if (!IsAlive)
                return;

            Durability = UnityEngine.Mathf.Max(0f, Durability - amount);
        }

        public bool TryAddCivilizationProgress(float amount, CivilizationBalanceConfig balance,
            out CivilizationStage advancedTo)
        {
            advancedTo = CivilizationStage;

            if (!IsAlive || !IsHabitable || Definition == null || !Definition.canCivilize)
                return false;

            var before = CivilizationStage;
            CivilizationProgress += amount;
            while (CivilizationUtility.CanProgress(CivilizationStage, balance) &&
                   CivilizationProgress >=
                   CivilizationUtility.GetProgressRequirement(CivilizationStage, balance))
            {
                CivilizationProgress -=
                    CivilizationUtility.GetProgressRequirement(CivilizationStage, balance);
                CivilizationStage = CivilizationUtility.Next(CivilizationStage);
            }

            if (CivilizationStage == before)
                return false;

            advancedTo = CivilizationStage;
            return true;
        }

        public void ForceLifeStage(CivilizationStage stage)
        {
            if (!IsHabitable)
                return;

            CivilizationStage = stage;
            CivilizationProgress = 0f;
        }

        public void AssignLifeIdentity(string planetName, string speciesName, string speciesDescription,
            string civilizationName, int intelligence, int aggression)
        {
            PlanetName = planetName;
            SpeciesName = speciesName;
            SpeciesDescription = speciesDescription;
            CivilizationName = civilizationName;
            Intelligence = intelligence;
            Aggression = aggression;
        }
    }
}
