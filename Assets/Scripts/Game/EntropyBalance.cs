namespace Universes.Game
{
    public static class EntropyBalance
    {
        public const float BaseEntropyPerSecond = 0.06f;
        public const float EntropyFromPassiveRate = 0.012f;
        public const float EntropyPerStardustProduced = 0.008f;
        public const float EntropyPerClick = 0.35f;
        public const float EntropyPerPassiveTick = 0.25f;
        public const float EntropyPerSupernova = 8f;
        public const float EntropyPerCollision = 14f;
        public const float EntropyPerBlackHolePerSecond = 0.42f;

        public static string GetStatusLabel(float entropy)
        {
            if (entropy >= 100f) return "Universe Collapsed";
            if (entropy >= 91f) return "Collapse Imminent";
            if (entropy >= 61f) return "Critical Universe";
            if (entropy >= 31f) return "Unstable Universe";
            return "Stable Universe";
        }
    }
}
