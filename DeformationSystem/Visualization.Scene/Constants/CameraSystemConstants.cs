namespace Visualization.Scene.Constants
{
    internal static class CameraSystemConstants
    {
        public const float ZoomToFitDistanceMultiplier = 1.05f;
        public const float ClipPlanesDistanceMultiplier = 5f;
        public const float MinNearClipPlaneDistance = 0.01f;
        public const float MinNearToFarClipPlaneDistance = 1000f;

        internal static class DefaultParameters
        {
            public const float DefaultPerspectiveFieldOfView = 60f;
            public const float DefaultOrthographicVerticalHeight = 5f;
            public const float DefaultNearClipPlane = 0.1f;
            public const float DefaultFarClipPlane = 1000f;
        }
    }
}
