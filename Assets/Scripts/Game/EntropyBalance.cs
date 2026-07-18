using UnityEngine;

namespace Universes.Game
{
    public static class EntropyBalance
    {
        public const float BaseEntropyPerSecond = 0.06f;
        public const float EntropyPerClick = 0.15f;
        public const float EntropyPerPassiveTick = 0.25f;
        public const float EntropyPerSupernova = 8f;
        public const float EntropyPerCollision = 14f;
        public const float EntropyPerBlackHolePerSecond = 0.42f;

        public static string GetStatusLabel(float entropy, float maxEntropy = 100f)
        {
            var max = Mathf.Max(1f, maxEntropy);
            var ratio = entropy / max;
            if (ratio >= 1f) return "Universe Collapsed";
            if (ratio >= 0.91f) return "Collapse Imminent";
            if (ratio >= 0.61f) return "Critical Universe";
            if (ratio >= 0.31f) return "Unstable Universe";
            return "Stable Universe";
        }
    }
}
