using UnityEngine;

namespace Universes.Prestige
{
    [CreateAssetMenu(fileName = "O_Variant", menuName = "Universes/Old/Parallel Variant")]
    public class O_ParallelVariant : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string bonusDescription;
        [TextArea] public string drawbackDescription;

        [Header("Multipliers")]
        public double stardustMultiplier = 1;
        public double entropyMultiplier = 1;
        public double lifeChanceMultiplier = 1;
        public double starLifespanMultiplier = 1;
        public double dnaMultiplier = 1;
    }
}
