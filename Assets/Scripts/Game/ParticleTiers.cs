using System.Collections.Generic;
using UnityEngine;

namespace Universes.Game
{
    public static class ParticleTiers
    {
        public static readonly int[] TierValues = { 100, 25, 5, 1 };

        public static List<int> SplitIntoChunks(int amount, float evolutionChance = 0f)
        {
            var chunks = new List<int>();
            var remaining = amount;

            foreach (var tier in TierValues)
            {
                while (remaining >= tier && chunks.Count < 48)
                {
                    var chunk = tier;
                    if (evolutionChance > 0f && Random.value < evolutionChance)
                        chunk = UpgradeChunk(chunk);

                    chunks.Add(chunk);
                    remaining -= tier;
                }
            }

            if (remaining > 0 && chunks.Count < 48)
            {
                var chunk = remaining;
                if (evolutionChance > 0f && Random.value < evolutionChance)
                    chunk = UpgradeChunk(chunk);

                chunks.Add(chunk);
            }

            return chunks;
        }

        private static int UpgradeChunk(int value) =>
            value switch
            {
                1 => 5,
                5 => 25,
                25 => 100,
                _ => value
            };

        public static float GetVisualScale(int value) =>
            value switch
            {
                >= 100 => 0.42f,
                >= 25 => 0.28f,
                >= 5 => 0.18f,
                _ => 0.1f
            };

        public static float GetFlySpeed(int value) =>
            value switch
            {
                >= 100 => 1.8f,
                >= 25 => 2.1f,
                >= 5 => 2.4f,
                _ => 2.7f
            };

        public static StarStage GetPreferredStageForTier(int value) =>
            value switch
            {
                >= 100 => StarStage.Supernova,
                >= 25 => StarStage.RedGiant,
                >= 5 => StarStage.Orange,
                _ => StarStage.Yellow
            };
    }
}
