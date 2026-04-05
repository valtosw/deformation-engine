using OpenTK.Mathematics;
using Visualization.Abstractions.Geometry;
using Visualization.Abstractions.Math;
using Visualization.Scene.Enums;

namespace Visualization.Scene.Abstractions
{
    public interface ICameraSystem
    {
        CameraMode CameraMode { get; set; }
        BoundingSphere TargetSphere { get; set; }
        Matrix4 ProjectionMatrix { get; }
        Matrix4 ViewMatrix { get; }

        void SetViewport(int width, int height);
        void Orbit(Vector2 oldMousePosition, Vector2 newMousePosition);
        void Pan(Vector2 oldMousePosition, Vector2 newMousePosition);
        void Zoom(float delta);
        void ZoomToFit();
        void SetViewPreset(ViewPreset viewPreset);
        Ray GetRay(Vector2 mousePosition);
    }
}