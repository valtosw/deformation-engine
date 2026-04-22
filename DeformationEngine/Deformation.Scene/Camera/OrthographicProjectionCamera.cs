using OpenTK.Mathematics;

namespace Deformation.Scene.Camera
{
    public sealed class OrthographicProjectionCamera(float verticalHeight, float nearClipPlane, float farClipPlane)
        : Camera(nearClipPlane, farClipPlane)
    {
        public float VerticalHeight { get; set; } = verticalHeight;

        public override Matrix4 GetProjectionMatrix(float aspectRatio)
        {
            var halfHeight = VerticalHeight * 0.5f;
            var halfWidth = halfHeight * aspectRatio;
            return Matrix4.CreateOrthographicOffCenter(-halfWidth, halfWidth, -halfHeight, halfHeight, NearClipPlane, FarClipPlane);
        }
    }
}