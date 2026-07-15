namespace Universes.Game
{
    public static class CameraZoomLimits
    {
        public static bool TryGetForPhase(GameController controller, out float minSize, out float maxSize)
        {
            minSize = 3f;
            maxSize = 15f;

            if (controller == null)
                return false;

            var config = controller.SingleStarBalance.multiStar;
            minSize = config.minZoom;
            maxSize = controller.GameplayMode switch
            {
                GameplayMode.SingleStarSystemAge => config.phase1MaxZoom,
                GameplayMode.MultiStarSystemAge => config.phase2MaxZoom,
                GameplayMode.FullCosmic => config.phase3MaxZoom,
                _ => config.phase1MaxZoom
            };

            if (minSize > maxSize)
                (minSize, maxSize) = (maxSize, minSize);

            return true;
        }
    }
}
