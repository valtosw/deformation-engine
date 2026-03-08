using OpenTK.Mathematics;
using Visualization.Scene.Nodes;

namespace Visualization.Scene
{
    public sealed class CameraSystem
    {
        private const float NearPlane = 0.1f;
        private const float FarPlane = 1000f;

        private readonly SceneNode _targetNode = new();
        private readonly SceneNode _viewNode = new();

        private readonly float _fieldOfView = float.DegreesToRadians(60f);
        private float _aspectRatio = 1f;

        public CameraSystem()
        {
            _targetNode.AddChild(_viewNode);
            _viewNode.Translation = new Vector3(0, 0, 10);
        }

        public Matrix4 ViewMatrix => _viewNode.WorldTransform.Inverted();
        public Matrix4 ProjectionMatrix { get; private set; } = Matrix4.Identity;

        public void SetViewport(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;

            _aspectRatio = (float)width / height;
            UpdateProjection();
        }

        public void Orbit(Vector2 delta)
        {
            var rotationX = Quaternion.FromAxisAngle(Vector3.UnitY, -delta.X);
            var rotationY = Quaternion.FromAxisAngle(_viewNode.WorldTransform.Column0.Xyz, -delta.Y);
            _targetNode.Rotation = rotationX * rotationY * _targetNode.Rotation;
        }

        public void Pan(Vector2 delta)
        {
            var right = _viewNode.WorldTransform.Column0.Xyz;
            var up = _viewNode.WorldTransform.Column1.Xyz;
            _targetNode.Translation += right * -delta.X + up * delta.Y;
        }

        public void Zoom(float delta)
        {
            var normalizedDelta = delta / 120f;
            var speed = _viewNode.Translation.Z * 0.1f;
            var newZ = Math.Max(0.1f, _viewNode.Translation.Z - normalizedDelta * speed);
            _viewNode.Translation = new Vector3(0, 0, newZ);
        }

        private void UpdateProjection()
            => ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(_fieldOfView, _aspectRatio, NearPlane, FarPlane);
    }
}
