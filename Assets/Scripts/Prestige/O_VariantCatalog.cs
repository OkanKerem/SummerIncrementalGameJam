using UnityEngine;

namespace Universes.Prestige
{
    [CreateAssetMenu(fileName = "O_VariantCatalog", menuName = "Universes/Old/Variant Catalog")]
    public class O_VariantCatalog : ScriptableObject
    {
        public O_ParallelVariant[] variants;

        public O_ParallelVariant GetById(string id)
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
