using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Math;
using Deformation.Interaction.Abstractions;
using Deformation.Interaction.Input;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;

namespace Deformation.Interaction
{
    public sealed class GizmoController(ICameraSystem cameraSystem) : IInputProcessor, IUpdater
    {
        #region Fields

        private SceneNode? _targetNode;
        private Axis? _activeAxis;
        private bool _isDragging;

        private Vector3 _lastIntersection;
        private Vector3 _gizmoStartCenter;
        private Vector3 _gizmoStartScale;

        #endregion

        #region Properties

        public GizmoNode GizmoNode { get; } = new();
        public bool IsEnabled { get; set; } = false;
        public GizmoMode Mode { get; set; } = GizmoMode.Translate;

        public SceneNode? TargetNode
        {
            get
            {
                return _targetNode;
            }
            set
            {
                _targetNode = value;
            }
        }

        #endregion

        #region Public Logic

        public void Update(float deltaTime)
        {
            if (TargetNode is not null && IsEnabled)
            {
                GizmoNode.IsVisible = true;
                GizmoNode.SetMode(Mode);

                if (!_isDragging)
                {
                    GizmoNode.Translation = TargetNode.BoundingBox.Center;
                    GizmoNode.Rotation = TargetNode.Rotation;

                    var size = TargetNode.BoundingBox.Size.Length;
                    GizmoNode.Scale = Vector3.One * MathF.Max(0.1f, size * 0.35f);
                }
                else
                {
                    GizmoNode.Translation = TargetNode.BoundingBox.Center;
                    GizmoNode.Rotation = TargetNode.Rotation;
                }
            }
            else
            {
                GizmoNode.IsVisible = false;
            }
        }

        public bool ProcessInput(IInputEvent inputEvent)
        {
            if (TargetNode is null || !IsEnabled)
            {
                return false;
            }

            return inputEvent switch
            {
                MouseClickEvent mouseClickEvent => HandleClick(mouseClickEvent),
                MouseMoveEvent mouseMoveEvent => HandleMove(mouseMoveEvent),
                _ => false
            };
        }

        #endregion

        #region Private Logic

        private bool HandleClick(MouseClickEvent mouseClickEvent)
        {
            if (mouseClickEvent.Button != MouseButton.Left)
            {
                return false;
            }

            if (mouseClickEvent.InputType == InputType.Up)
            {
                if (_isDragging)
                {
                    _isDragging = false;
                    _activeAxis = null;
                    return true;
                }

                return false;
            }

            var ray = cameraSystem.GetRay(mouseClickEvent.Position);

            var xAxis = GizmoNode.GetActiveXAxis(Mode);
            var yAxis = GizmoNode.GetActiveYAxis(Mode);
            var zAxis = GizmoNode.GetActiveZAxis(Mode);

            var hitX = CheckIntersection(ray, xAxis, out var distanceX);
            var hitY = CheckIntersection(ray, yAxis, out var distanceY);
            var hitZ = CheckIntersection(ray, zAxis, out var distanceZ);

            var minimumDistance = float.MaxValue;
            Axis? bestAxis = null;

            if (hitX && distanceX < minimumDistance)
            {
                minimumDistance = distanceX;
                bestAxis = Axis.X;
            }

            if (hitY && distanceY < minimumDistance)
            {
                minimumDistance = distanceY;
                bestAxis = Axis.Y;
            }

            if (hitZ && distanceZ < minimumDistance)
            {
                bestAxis = Axis.Z;
            }

            if (bestAxis is not null)
            {
                _activeAxis = bestAxis;
                return StartDrag(ray, bestAxis.Value);
            }

            return false;
        }

        private bool StartDrag(Ray ray, Axis axis)
        {
            _isDragging = true;
            _gizmoStartCenter = GizmoNode.Translation;
            _gizmoStartScale = GizmoNode.Scale;

            var axisDirection = GetWorldAxisDirection(axis);
            Plane plane;

            if (Mode == GizmoMode.Rotate)
            {
                plane = new Plane(axisDirection, _gizmoStartCenter);
            }
            else
            {
                var planeNormal = Vector3.Cross(Vector3.Cross(ray.Direction, axisDirection), axisDirection).Normalized();

                if (planeNormal.LengthSquared < 0.0001f)
                {
                    var up = Vector3.Cross(axisDirection, Vector3.UnitY);

                    if (up.LengthSquared < 0.0001f)
                    {
                        up = Vector3.Cross(axisDirection, Vector3.UnitX);
                    }

                    planeNormal = up.Normalized();
                }

                plane = new Plane(planeNormal, _gizmoStartCenter);
            }

            var intersection = ray.Intersects(plane);

            if (intersection.HasValue)
            {
                _lastIntersection = intersection.Value;
                return true;
            }

            return false;
        }

        private bool HandleMove(MouseMoveEvent mouseMoveEvent)
        {
            if (!_isDragging || _activeAxis is null || TargetNode is null)
            {
                return false;
            }

            var ray = cameraSystem.GetRay(mouseMoveEvent.Position);
            var axisDirection = GetWorldAxisDirection(_activeAxis.Value);
            Plane plane;

            if (Mode == GizmoMode.Rotate)
            {
                plane = new Plane(axisDirection, _gizmoStartCenter);
            }
            else
            {
                var planeNormal = Vector3.Cross(Vector3.Cross(ray.Direction, axisDirection), axisDirection).Normalized();

                if (planeNormal.LengthSquared < 0.0001f)
                {
                    var up = Vector3.Cross(axisDirection, Vector3.UnitY);

                    if (up.LengthSquared < 0.0001f)
                    {
                        up = Vector3.Cross(axisDirection, Vector3.UnitX);
                    }

                    planeNormal = up.Normalized();
                }

                plane = new Plane(planeNormal, _gizmoStartCenter);
            }

            var intersection = ray.Intersects(plane);

            if (intersection.HasValue)
            {
                var currentIntersection = intersection.Value;
                var localAxis = GetLocalAxis(_activeAxis.Value);

                if (Mode == GizmoMode.Rotate)
                {
                    var startVector = (_lastIntersection - _gizmoStartCenter).Normalized();
                    var currentVector = (currentIntersection - _gizmoStartCenter).Normalized();

                    var cross = Vector3.Cross(startVector, currentVector);
                    var dot = MathHelper.Clamp(Vector3.Dot(startVector, currentVector), -1f, 1f);
                    var angle = MathF.Atan2(cross.Length, dot);

                    if (Vector3.Dot(cross, axisDirection) < 0)
                    {
                        angle = -angle;
                    }

                    var rotationDelta = Quaternion.FromAxisAngle(localAxis, angle);

                    var offset = TargetNode.Translation - _gizmoStartCenter;
                    var rotatedOffset = Vector3.Transform(offset, rotationDelta);

                    TargetNode.Translation = _gizmoStartCenter + rotatedOffset;
                    TargetNode.Rotation *= rotationDelta;
                }
                else if (Mode == GizmoMode.Translate)
                {
                    var delta = currentIntersection - _lastIntersection;
                    var projection = Vector3.Dot(delta, axisDirection);

                    TargetNode.Translation += axisDirection * projection;
                }
                else if (Mode == GizmoMode.Scale)
                {
                    var delta = currentIntersection - _lastIntersection;
                    var projection = Vector3.Dot(delta, axisDirection);
                    var scaleMultiplier = projection / _gizmoStartScale.X;

                    var newScale = TargetNode.Scale + localAxis * scaleMultiplier;

                    newScale.X = MathF.Max(0.01f, newScale.X);
                    newScale.Y = MathF.Max(0.01f, newScale.Y);
                    newScale.Z = MathF.Max(0.01f, newScale.Z);

                    TargetNode.Scale = newScale;
                }

                _lastIntersection = currentIntersection;
            }

            return true;
        }

        private Vector3 GetWorldAxisDirection(Axis axis)
        {
            return GizmoNode.WorldTransform.TransformDirection(GetLocalAxis(axis)).Normalized();
        }

        private static Vector3 GetLocalAxis(Axis axis)
        {
            return axis switch
            {
                Axis.X => Vector3.UnitX,
                Axis.Y => Vector3.UnitY,
                Axis.Z => Vector3.UnitZ,
                _ => Vector3.UnitY
            };
        }

        private static bool CheckIntersection(Ray ray, MeshNode axisNode, out float distance)
        {
            distance = float.MaxValue;

            if (axisNode.Mesh?.LocalBoundingBox is null)
            {
                return false;
            }

            var inverseTransform = axisNode.WorldTransform.Inverted();
            var localRay = ray.Transformed(inverseTransform);

            return localRay.Intersects(axisNode.Mesh.LocalBoundingBox, out distance);
        }

        #endregion
    }
}