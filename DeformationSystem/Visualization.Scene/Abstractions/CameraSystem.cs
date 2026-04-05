using OpenTK.Mathematics;
using Visualization.Abstractions.Constants;
using Visualization.Abstractions.Extensions;
using Visualization.Abstractions.Geometry;
using Visualization.Abstractions.Math;
using Visualization.Scene.Constants;
using Visualization.Scene.Enums;
using Visualization.Scene.Nodes;

namespace Visualization.Scene.Abstractions
{
    public sealed class CameraSystem : ICameraSystem
    {
        private readonly SceneNode _targetNode = new();
        private readonly SceneNode _viewNode = new();

        private readonly PerspectiveProjectionCamera _perspectiveCamera;
        private readonly OrthographicProjectionCamera _orthographicCamera;

        private BoundingSphere _targetSphere = new(Vector3.Zero, 1f);
        private BoundingSphere _baseTargetSphere = new(Vector3.Zero, 1f);
        private float _aspectRatio = 1f;
        private int _viewportWidth = 1;
        private int _viewportHeight = 1;

        public CameraSystem()
        {
            _perspectiveCamera = new PerspectiveProjectionCamera(
                fovDegrees: CameraSystemConstants.DefaultParameters.DefaultPerspectiveFieldOfView,
                nearClipPlane: CameraSystemConstants.DefaultParameters.DefaultNearClipPlane,
                farClipPlane: CameraSystemConstants.DefaultParameters.DefaultFarClipPlane);

            _orthographicCamera = new OrthographicProjectionCamera(
                verticalHeight: CameraSystemConstants.DefaultParameters.DefaultOrthographicVerticalHeight,
                nearClipPlane: CameraSystemConstants.DefaultParameters.DefaultNearClipPlane,
                farClipPlane: CameraSystemConstants.DefaultParameters.DefaultFarClipPlane);

            _targetNode.AddChild(_viewNode);
            UpdateCamera();
        }

        public CameraMode CameraMode { get; set; } = CameraMode.Perspective;

        public BoundingSphere TargetSphere
        {
            get => _targetSphere;
            set
            {
                _baseTargetSphere = value;
                _targetSphere = value;
                UpdateCamera();
            }
        }

        public Matrix4 ProjectionMatrix => CameraMode == CameraMode.Perspective
            ? _perspectiveCamera.GetProjectionMatrix(_aspectRatio)
            : _orthographicCamera.GetProjectionMatrix(_aspectRatio);

        public Matrix4 ViewMatrix => _viewNode.WorldTransform.Inverted();

        public void SetViewport(int width, int height)
        {
            _viewportWidth = Math.Max(1, width);
            _viewportHeight = Math.Max(1, height);
            _aspectRatio = (float)_viewportWidth / _viewportHeight;
            UpdateCamera();
        }

        public void Orbit(Vector2 oldMousePosition, Vector2 newMousePosition)
        {
            var vector1 = MapToArcBall(oldMousePosition);
            var vector2 = MapToArcBall(newMousePosition);

            var axis = Vector3.Cross(vector2, vector1);
            var dot = Vector3.Dot(vector1, vector2);
            var angle = MathF.Atan2(axis.Length, MathHelper.Clamp(dot, -1f, 1f));

            if (axis.LengthSquared <= MathConstants.LengthTolerance)
                return;

            var worldAxis = _viewNode.WorldTransform.TransformDirection(axis.Normalized()).Normalized();
            _targetNode.Rotation *= Quaternion.FromAxisAngle(worldAxis, angle);
        }

        public void Pan(Vector2 oldMousePosition, Vector2 newMousePosition)
        {
            var worldOld = UnprojectToTargetPlane(oldMousePosition);
            var worldNew = UnprojectToTargetPlane(newMousePosition);

            if (worldOld is null || worldNew is null)
                return;

            _targetNode.Translation += worldOld.Value - worldNew.Value;
            _targetSphere = _targetSphere with { Center = _targetNode.Translation };
        }

        public void Zoom(float delta)
        {
            var scale = MathF.Pow(1.1f, MathF.Abs(delta * 0.01f));
            var newRadius = delta > 0
                ? _targetSphere.Radius / scale
                : _targetSphere.Radius * scale;

            _targetSphere = _targetSphere with { Radius = newRadius };
            UpdateCamera();
        }

        public void ZoomToFit()
        {
            _targetSphere = _baseTargetSphere;
            UpdateCamera();
        }

        public void SetViewPreset(ViewPreset viewPreset)
        {
            _targetNode.Rotation = viewPreset switch
            {
                ViewPreset.Front     => Quaternion.Identity,
                ViewPreset.Back      => Quaternion.FromEulerAngles(0, MathHelper.Pi, 0),
                ViewPreset.Left      => Quaternion.FromEulerAngles(0, -MathHelper.PiOver2, 0),
                ViewPreset.Right     => Quaternion.FromEulerAngles(0, MathHelper.PiOver2, 0),
                ViewPreset.Top       => Quaternion.FromEulerAngles(-MathHelper.PiOver2, 0, 0),
                ViewPreset.Bottom    => Quaternion.FromEulerAngles(MathHelper.PiOver2, 0, 0),
                ViewPreset.Isometric => Quaternion.FromEulerAngles(MathF.Atan(-1f / MathF.Sqrt(2f)), MathHelper.PiOver4, 0),
                _                    => _targetNode.Rotation
            };

            UpdateCamera();
        }

        private void UpdateCamera()
        {
            var fieldOfViewInRadians = MathHelper.DegreesToRadians(_perspectiveCamera.FieldOfView);
            var distance = _targetSphere.Radius / MathF.Sin(fieldOfViewInRadians / 2f) * CameraSystemConstants.ZoomToFitDistanceMultiplier;

            _targetNode.Translation = _targetSphere.Center;
            _viewNode.Translation = new Vector3(0, 0, distance);

            _orthographicCamera.VerticalHeight = 2f * (_aspectRatio < 1f ? _targetSphere.Radius / _aspectRatio : _targetSphere.Radius);

            var nearClipPlane = Math.Max(CameraSystemConstants.MinNearClipPlaneDistance, distance - _targetSphere.Radius * 2f);
            var farClipPlane = distance + _targetSphere.Radius * CameraSystemConstants.ClipPlanesDistanceMultiplier;

            _perspectiveCamera.NearClipPlane = _orthographicCamera.NearClipPlane = nearClipPlane;
            _perspectiveCamera.FarClipPlane = _orthographicCamera.FarClipPlane = farClipPlane;
        }

        private Vector3? UnprojectToTargetPlane(Vector2 mousePos)
        {
            var ray = GetRay(mousePos);
            var planeNormal = _viewNode.WorldTransform.TransformDirection(Vector3.UnitZ).Normalized();
            var denominator = Vector3.Dot(planeNormal, ray.Direction);

            if (MathF.Abs(denominator) < MathConstants.ZeroTolerance)
                return null;

            var parameter = Vector3.Dot(_targetSphere.Center - ray.Origin, planeNormal) / denominator;

            return ray.Origin + ray.Direction * parameter;
        }

        public Ray GetRay(Vector2 mousePosition)
        {
            var x = 2f * mousePosition.X / _viewportWidth - 1f;
            var y = 1f - 2f * mousePosition.Y / _viewportHeight;

            var invertedViewProjection = (ViewMatrix * ProjectionMatrix).Inverted();
            var nearClipPoint = invertedViewProjection.TransformPoint(new Vector3(x, y, -1f));
            var farClipPoint = invertedViewProjection.TransformPoint(new Vector3(x, y, 1f));

            return new Ray(nearClipPoint, (farClipPoint - nearClipPoint).Normalized());
        }

        private Vector3 MapToArcBall(Vector2 mousePosition)
        {
            var x = (2f * mousePosition.X / _viewportWidth - 1f) * _aspectRatio;
            var y = 1f - 2f * mousePosition.Y / _viewportHeight;
            var lengthSquared = x * x + y * y;

            return lengthSquared > 1f
                ? new Vector3(x, y, 0).Normalized()
                : new Vector3(x, y, MathF.Sqrt(1f - lengthSquared)).Normalized();
        }
    }
}