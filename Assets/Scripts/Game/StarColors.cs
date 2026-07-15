using UnityEngine;

namespace Universes.Game
{
    public static class StarColors
    {
        public static Color GetStageColor(StarStage stage) =>
            stage switch
            {
                StarStage.Yellow => new Color(1f, 0.92f, 0.35f),
                StarStage.Orange => new Color(1f, 0.55f, 0.18f),
                StarStage.RedGiant => new Color(0.95f, 0.22f, 0.18f),
                StarStage.Supernova => new Color(1f, 0.98f, 0.9f),
                _ => Color.white
            };

        public static Color GetGlowColor(StarStage stage)
        {
            var c = GetStageColor(stage);
            c.a = stage switch
            {
                StarStage.Yellow => 0.45f,
                StarStage.Orange => 0.5f,
                StarStage.RedGiant => 0.55f,
                StarStage.Supernova => 0.7f,
                _ => 0.4f
            };
            return c;
        }

        public static float GetStageScale(StarStage stage) =>
            stage switch
            {
                StarStage.RedGiant => 1.35f,
                StarStage.Supernova => 1.6f,
                _ => 1f
            };

        public static int StageToAnimatorId(StarStage stage) =>
            stage switch
            {
                StarStage.Yellow => 0,
                StarStage.Orange => 1,
                StarStage.RedGiant => 2,
                StarStage.Supernova => 3,
                _ => 0
            };
    }
}
