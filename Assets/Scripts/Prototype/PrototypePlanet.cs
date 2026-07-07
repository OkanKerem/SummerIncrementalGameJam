namespace Universes.Prototype
{
    public class PrototypePlanet
    {
        public int Id { get; }
        public int HostStarId { get; }
        public PrototypePlanetTypeDefinition Definition { get; }
        public PrototypePlanetType Type => Definition != null ? Definition.planetType : PrototypePlanetType.Rocky;
        public int OrbitSlot { get; }
        public float OrbitRadius { get; }
        public float OrbitAngle { get; set; }
        public float Durability { get; private set; }
        public float MaxDurability { get; }
        public bool IsHabitable { get; }
        public PrototypeCivilizationStage CivilizationStage { get; private set; }
        public float CivilizationProgress { get; private set; }
        public string PlanetName { get; private set; }
        public string SpeciesName { get; private set; }
        public string SpeciesDescription { get; private set; }
        public string CivilizationName { get; private set; }
        public int Intelligence { get; private set; }
        public int Aggression { get; private set; }

        public bool IsAlive => Durability > 0f;
        public bool HasLife => CivilizationStage > PrototypeCivilizationStage.NoLife;
        public bool HasSpecies => !string.IsNullOrWhiteSpace(SpeciesName);
        public bool LifeCountedForStats { get; set; }

        public PrototypePlanet(int id, int hostStarId, PrototypePlanetTypeDefinition definition, int orbitSlot,
            float orbitRadius, float orbitAngle, bool isHabitable, float fallbackMaxDurability)
        {
            Id = id;
            HostStarId = hostStarId;
            Definition = definition;
            OrbitSlot = orbitSlot;
            OrbitRadius = orbitRadius;
            OrbitAngle = orbitAngle;
            IsHabitable = isHabitable;

            MaxDurability = definition != null ? definition.maxDurability : fallbackMaxDurability;
            Durability = MaxDurability;
            CivilizationStage = PrototypeCivilizationStage.NoLife;
        }

        public void Damage(float amount)
        {
            if (!IsAlive)
                return;

            Durability = UnityEngine.Mathf.Max(0f, Durability - amount);
        }

        public bool TryAddCivilizationProgress(float amount, PrototypeCivilizationBalanceConfig balance,
            out PrototypeCivilizationStage advancedTo)
        {
            advancedTo = CivilizationStage;

            if (!IsAlive || !IsHabitable || Definition == null || !Definition.canCivilize)
                return false;

            var before = CivilizationStage;
            CivilizationProgress += amount;
            while (PrototypeCivilizationUtility.CanProgress(CivilizationStage, balance) &&
                   CivilizationProgress >=
                   PrototypeCivilizationUtility.GetProgressRequirement(CivilizationStage, balance))
            {
                CivilizationProgress -=
                    PrototypeCivilizationUtility.GetProgressRequirement(CivilizationStage, balance);
                CivilizationStage = PrototypeCivilizationUtility.Next(CivilizationStage);
            }

            if (CivilizationStage == before)
                return false;

            advancedTo = CivilizationStage;
            return true;
        }

        public void ForceLifeStage(PrototypeCivilizationStage stage)
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
