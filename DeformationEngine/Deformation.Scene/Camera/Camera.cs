using OpenTK.Mathematics;

namespace Deformation.Scene.Camera
{
    public abstract class Camera(float nearClipPlane, float farClipPlane)
    {
        public float NearClipPlane { get; set; } = nearClipPlane;
        public float FarClipPlane { get; set; } = farClipPlane;

        public abstract Matrix4 GetProjectionMatrix(float aspectRatio);
    }
}
