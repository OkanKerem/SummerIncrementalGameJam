using System.Collections.Generic;
using UnityEngine;

namespace Universes.Prototype
{
    [CreateAssetMenu(fileName = "PlanetTypeCatalog", menuName = "Universes/Prototype Planet Type Catalog")]
    public class PrototypePlanetTypeCatalog : ScriptableObject
    {
        [SerializeField] private List<PrototypePlanetTypeDefinition> planetTypes = new();

        public IReadOnlyList<PrototypePlanetTypeDefinition> PlanetTypes => planetTypes;

        public PrototypePlanetTypeDefinition RollRandom()
        {
            if (planetTypes == null || planetTypes.Count == 0)
                return null;

            var total = 0f;
            foreach (var type in planetTypes)
            {
                if (type != null)
                    total += Mathf.Max(0f, type.spawnWeight);
            }

            if (total <= 0f)
                return planetTypes[0];

            var roll = Random.value * total;
            foreach (var type in planetTypes)
            {
                if (type == null)
                    continue;

                roll -= Mathf.Max(0f, type.spawnWeight);
                if (roll <= 0f)
                    return type;
            }

            return planetTypes[planetTypes.Count - 1];
        }

        public PrototypePlanetTypeDefinition GetByType(PrototypePlanetType type)
        {
            if (planetTypes == null)
                return null;

            foreach (var definition in planetTypes)
            {
                if (definition != null && definition.planetType == type)
                    return definition;
            }

            return null;
        }
    }
}
