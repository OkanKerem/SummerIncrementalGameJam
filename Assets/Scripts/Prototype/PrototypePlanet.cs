namespace Universes.Prototype
{
    public class PrototypePlanet
    {
        public int Id { get; }
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

        public bool IsAlive => Durability > 0f;
        public bool HasLife => CivilizationStage > PrototypeCivilizationStage.NoLife;
        public bool LifeCountedForStats { get; set; }

        public PrototypePlanet(int id, PrototypePlanetTypeDefinition definition, int orbitSlot,
            float orbitRadius, float orbitAngle, bool isHabitable)
        {
            Id = id;
            Definition = definition;
            OrbitSlot = orbitSlot;
            OrbitRadius = orbitRadius;
            OrbitAngle = orbitAngle;
            IsHabitable = isHabitable;

            MaxDurability = definition != null ? definition.maxDurability : 100f;
            Durability = MaxDurability;
            CivilizationStage = PrototypeCivilizationStage.NoLife;
        }

        public void Damage(float amount)
        {
            if (!IsAlive)
                return;

            Durability = UnityEngine.Mathf.Max(0f, Durability - amount);
        }

        public bool TryAddCivilizationProgress(float amount, out PrototypeCivilizationStage advancedTo)
        {
            advancedTo = CivilizationStage;

            if (!IsAlive || !IsHabitable || Definition == null || !Definition.canCivilize)
                return false;

            var before = CivilizationStage;
            CivilizationProgress += amount;
            while (CivilizationProgress >= 1f && PrototypeCivilizationUtility.CanProgress(CivilizationStage))
            {
                CivilizationProgress -= 1f;
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
    }
}
