using UnityEngine;

namespace Universes.Prestige
{
    [CreateAssetMenu(fileName = "VariantCatalog", menuName = "Universes/Variant Catalog")]
    public class VariantCatalog : ScriptableObject
    {
        public ParallelVariant[] variants;

        public ParallelVariant GetById(string id)
        {
            if (variants == null)
                return null;

            foreach (var v in variants)
            {
                if (v != null && v.id == id)
                    return v;
            }

            return variants.Length > 0 ? variants[0] : null;
        }
    }
}
