using System.Collections.Generic;
using UnityEngine;

namespace Universes.Game
{
    [CreateAssetMenu(fileName = "PlanetTypeCatalog", menuName = "Universes/Planet Type Catalog")]
    public class PlanetTypeCatalog : ScriptableObject
    {
        [SerializeField] private List<PlanetTypeDefinition> planetTypes = new();

        public IReadOnlyList<PlanetTypeDefinition> PlanetTypes => planetTypes;

        public PlanetTypeDefinition RollRandom()
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

        public PlanetTypeDefinition GetByType(PlanetType type)
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
