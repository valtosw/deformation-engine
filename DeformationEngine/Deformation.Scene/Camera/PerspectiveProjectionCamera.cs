using OpenTK.Mathematics;

namespace Deformation.Scene.Camera
{
    public sealed class PerspectiveProjectionCamera(float fovDegrees, float nearClipPlane, float farClipPlane)
        : Camera(nearClipPlane, farClipPlane)
    {
        public float FieldOfView { get; set; } = fovDegrees;

        public override Matrix4 GetProjectionMatrix(float aspectRatio)
        {
            return Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FieldOfView), aspectRatio, NearClipPlane, FarClipPlane);
        }
    }
}