using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Math;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Constants;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;

namespace Deformation.Scene
{
    public sealed class GizmoSystem : IGizmoSystem
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
        public bool IsEnabled { get; set; }
        public GizmoMode Mode { get; set; } = GizmoMode.Translate;
        public float BoneGizmoRadius { get; set; }

        public SceneNode? TargetNode
        {
            get
            {
                return _targetNode;
            }
            set
            {
                if (_targetNode == value)
                {
                    return;
                }

                EndDrag();
                _targetNode = value;
            }
        }

        #endregion

        #region Public Logic

        public void Update(float deltaTime)
        {
            if (TargetNode is not null && IsEnabled)
            {
                var activeMode = GetActiveMode();

                GizmoNode.IsVisible = true;
                GizmoNode.SetMode(activeMode);

                if (!_isDragging)
                {
                    GizmoNode.Translation = GetTargetCenter(TargetNode);
                    GizmoNode.Rotation = GetTargetRotation(TargetNode);

                    var size = GetScaleReferenceSize();
                    GizmoNode.Scale = Vector3.One * GetTargetScale(size, TargetNode);
                }
                else
                {
                    GizmoNode.Translation = GetTargetCenter(TargetNode);
                    GizmoNode.Rotation = GetTargetRotation(TargetNode);
                }
            }
            else
            {
                GizmoNode.IsVisible = false;
            }
        }

        public bool StartDrag(Ray ray)
        {
            var activeMode = GetActiveMode();
            var xAxis = GizmoNode.GetActiveXAxis(activeMode);
            var yAxis = GizmoNode.GetActiveYAxis(activeMode);
            var zAxis = GizmoNode.GetActiveZAxis(activeMode);

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
                return BeginAxisDrag(ray, bestAxis.Value);
            }

            return false;
        }

        public bool UpdateDrag(Ray ray)
        {
            if (!_isDragging || _activeAxis is null || TargetNode is null)
            {
                return false;
            }

            var axisDirection = GetWorldAxisDirection(_activeAxis.Value);
            var plane = CalculateDragPlane(ray, axisDirection);
            var intersection = ray.Intersects(plane);

            if (intersection.HasValue)
            {
                ApplyDrag(intersection.Value, axisDirection);
                _lastIntersection = intersection.Value;
            }

            return true;
        }

        public bool EndDrag()
        {
            if (_isDragging)
            {
                _isDragging = false;
                _activeAxis = null;

                return true;
            }

            return false;
        }

        #endregion

        #region Private Logic

        private bool BeginAxisDrag(Ray ray, Axis axis)
        {
            _isDragging = true;
            _gizmoStartCenter = GizmoNode.Translation;
            _gizmoStartScale = GizmoNode.Scale;

            var axisDirection = GetWorldAxisDirection(axis);
            var plane = CalculateDragPlane(ray, axisDirection);
            var intersection = ray.Intersects(plane);

            if (intersection.HasValue)
            {
                _lastIntersection = intersection.Value;
                return true;
            }

            return false;
        }

        private Plane CalculateDragPlane(Ray ray, Vector3 axisDirection)
        {
            if (GetActiveMode() == GizmoMode.Rotate)
            {
                return new Plane(axisDirection, _gizmoStartCenter);
            }

            var planeNormal = Vector3.Cross(Vector3.Cross(ray.Direction, axisDirection), axisDirection).Normalized();

            if (planeNormal.LengthSquared < MathConstants.LengthTolerance)
            {
                var up = Vector3.Cross(axisDirection, Vector3.UnitY);

                if (up.LengthSquared < MathConstants.LengthTolerance)
                {
                    up = Vector3.Cross(axisDirection, Vector3.UnitX);
                }

                planeNormal = up.Normalized();
            }

            return new Plane(planeNormal, _gizmoStartCenter);
        }

        private void ApplyDrag(Vector3 currentIntersection, Vector3 axisDirection)
        {
            if (TargetNode is null || _activeAxis is null)
            {
                return;
            }

            var localAxis = GetLocalAxis(_activeAxis.Value);

            var activeMode = GetActiveMode();

            if (activeMode == GizmoMode.Rotate)
            {
                var startVector = (_lastIntersection - _gizmoStartCenter).Normalized();
                var currentVector = (currentIntersection - _gizmoStartCenter).Normalized();

                var cross = Vector3.Cross(startVector, currentVector);
                var dot = MathHelper.Clamp(Vector3.Dot(startVector, currentVector), -1f, 1f);
                var angle = MathF.Atan2(cross.Length, dot);

                if (Vector3.Dot(cross, axisDirection) < MathConstants.ZeroTolerance)
                {
                    angle = -angle;
                }

                var parentWorldTransform = TargetNode.Parent?.WorldTransform ?? Matrix4.Identity;
                var inverseParentTransform = parentWorldTransform.Inverted();
                var localRotationAxis = inverseParentTransform.TransformDirection(axisDirection).Normalized();
                var rotationDelta = Quaternion.FromAxisAngle(localRotationAxis, angle);

                if (TargetNode is not BoneNode)
                {
                    var worldRotationDelta = Quaternion.FromAxisAngle(axisDirection, angle);
                    var targetWorldPosition = TargetNode.WorldTransform.ExtractTranslation();
                    var offset = targetWorldPosition - _gizmoStartCenter;
                    var rotatedOffset = Vector3.Transform(offset, worldRotationDelta);
                    var newWorldPosition = _gizmoStartCenter + rotatedOffset;
                    var newLocalPosition = inverseParentTransform.TransformPoint(newWorldPosition);
                    TargetNode.Translation = newLocalPosition;
                }

                TargetNode.Rotation = rotationDelta * TargetNode.Rotation;
            }
            else if (activeMode == GizmoMode.Translate)
            {
                var delta = currentIntersection - _lastIntersection;
                var projection = Vector3.Dot(delta, axisDirection);

                TargetNode.Translation += axisDirection * projection;
            }
            else if (activeMode == GizmoMode.Scale)
            {
                var delta = currentIntersection - _lastIntersection;
                var projection = Vector3.Dot(delta, axisDirection);
                var scaleMultiplier = projection / _gizmoStartScale.X;

                var newScale = TargetNode.Scale + localAxis * scaleMultiplier;

                newScale.X = MathF.Max(GizmoConstants.MinimumScale, newScale.X);
                newScale.Y = MathF.Max(GizmoConstants.MinimumScale, newScale.Y);
                newScale.Z = MathF.Max(GizmoConstants.MinimumScale, newScale.Z);

                TargetNode.Scale = newScale;
            }
        }

        private Vector3 GetWorldAxisDirection(Axis axis)
        {
            return GizmoNode.WorldTransform.TransformDirection(GetLocalAxis(axis)).Normalized();
        }

        private GizmoMode GetActiveMode()
        {
            return TargetNode is ControlPointNode ? GizmoMode.Translate : Mode;
        }

        private float GetScaleReferenceSize()
        {
            if (TargetNode is ControlPointNode && TargetNode.Parent is not null)
            {
                return TargetNode.Parent.BoundingBox.Size.Length;
            }

            return TargetNode?.BoundingBox.Size.Length ?? 1f;
        }

        private float GetTargetScale(float size, SceneNode targetNode)
        {
            if (targetNode is BoneNode)
            {
                var tightWrapScale = BoneGizmoRadius * GizmoConstants.TightWrapScaleMultiplier;
                return MathF.Max(GizmoConstants.MinimumScale, tightWrapScale);
            }

            var scaleFactor = GizmoConstants.DefaultScaleFactor;

            if (targetNode is ControlPointNode)
            {
                scaleFactor = GizmoConstants.ControlPointScaleFactor;
            }

            return MathF.Max(GizmoConstants.MinimumVisualScale, size * scaleFactor);
        }

        private static Vector3 GetTargetCenter(SceneNode targetNode)
        {
            if (targetNode is BoneNode)
            {
                return targetNode.WorldTransform.ExtractTranslation();
            }

            return targetNode.BoundingBox.Center;
        }

        private static Quaternion GetTargetRotation(SceneNode targetNode)
        {
            if (targetNode is BoneNode)
            {
                return targetNode.WorldTransform.ExtractRotation();
            }

            return targetNode.Rotation;
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