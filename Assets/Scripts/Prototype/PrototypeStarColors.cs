using UnityEngine;

namespace Universes.Prototype
{
    public static class PrototypeStarColors
    {
        public static Color GetStageColor(PrototypeStarStage stage) =>
            stage switch
            {
                PrototypeStarStage.Yellow => new Color(1f, 0.92f, 0.35f),
                PrototypeStarStage.Orange => new Color(1f, 0.55f, 0.18f),
                PrototypeStarStage.RedGiant => new Color(0.95f, 0.22f, 0.18f),
                PrototypeStarStage.Supernova => new Color(1f, 0.98f, 0.9f),
                _ => Color.white
            };

        public static Color GetGlowColor(PrototypeStarStage stage)
        {
            var c = GetStageColor(stage);
            c.a = stage switch
            {
                PrototypeStarStage.Yellow => 0.45f,
                PrototypeStarStage.Orange => 0.5f,
                PrototypeStarStage.RedGiant => 0.55f,
                PrototypeStarStage.Supernova => 0.7f,
                _ => 0.4f
            };
            return c;
        }

        public static float GetStageScale(PrototypeStarStage stage) =>
            stage switch
            {
                PrototypeStarStage.RedGiant => 1.35f,
                PrototypeStarStage.Supernova => 1.6f,
                _ => 1f
            };

        public static int StageToAnimatorId(PrototypeStarStage stage) =>
            stage switch
            {
                PrototypeStarStage.Yellow => 0,
                PrototypeStarStage.Orange => 1,
                PrototypeStarStage.RedGiant => 2,
                PrototypeStarStage.Supernova => 3,
                _ => 0
            };
    }
}
