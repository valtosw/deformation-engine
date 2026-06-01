using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Abstractions.Math;
using Deformation.Interaction.Abstractions;
using Deformation.Interaction.Input;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;

namespace Deformation.Interaction
{
    public sealed class ArapSelectionController(
        ICameraSystem cameraSystem,
        Func<bool> isEnabled,
        Func<MeshNode?> meshProvider,
        Func<ArapDeformer> deformerProvider,
        Func<float> brushRadiusProvider,
        Action onSelectionChanged) : IInputProcessor
    {
        #region Fields

        private bool _isPainting;

        #endregion

        #region Public Logic

        public bool ProcessInput(IInputEvent inputEvent)
        {
            if (!isEnabled())
            {
                _isPainting = false;
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

            var deformer = deformerProvider();

            if (deformer.ActionMode == ArapActionMode.Deform)
            {
                _isPainting = false;
                return false;
            }

            _isPainting = mouseClickEvent.InputType == InputType.Down;

            if (!_isPainting)
            {
                return false;
            }

            Paint(mouseClickEvent.Position, mouseClickEvent.IsErase);
            return true;
        }

        private bool HandleMove(MouseMoveEvent mouseMoveEvent)
        {
            if (!_isPainting)
            {
                return false;
            }

            Paint(mouseMoveEvent.Position, mouseMoveEvent.IsErase);
            return true;
        }

        private void Paint(Vector2 screenPosition, bool erase)
        {
            var meshNode = meshProvider();
            var mesh = meshNode?.Mesh;

            if (meshNode is null || mesh is null)
            {
                return;
            }

            var ray = cameraSystem.GetRay(screenPosition).Transformed(meshNode.WorldTransform.Inverted());

            var brushRadius = brushRadiusProvider();

            if (!TryFindHitPoint(mesh, ray, brushRadius, out var hitPoint))
            {
                return;
            }

            var deformer = deformerProvider();
            var vertices = deformer.GetVerticesWithinBrush(hitPoint, brushRadius);
            deformer.PaintVertices(vertices, erase);
            onSelectionChanged();
        }

        private static bool TryFindHitPoint(Mesh mesh, Ray ray, float brushRadius, out Vector3 hitPoint)
        {
            hitPoint = Vector3.Zero;

            if (mesh.Topology == MeshTopology.Lines)
            {
                return TryFindLineHitPoint(mesh, ray, brushRadius, out hitPoint);
            }

            var closestDistance = float.MaxValue;
            var hasHit = false;

            for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
            {
                var p0 = mesh.Vertices[(int)mesh.Indices[index]].Position;
                var p1 = mesh.Vertices[(int)mesh.Indices[index + 1]].Position;
                var p2 = mesh.Vertices[(int)mesh.Indices[index + 2]].Position;

                if (!IntersectsTriangle(ray, p0, p1, p2, out var distance) || distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                hitPoint = ray.Origin + ray.Direction * distance;
                hasHit = true;
            }

            return hasHit;
        }

        private static bool TryFindLineHitPoint(Mesh mesh, Ray ray, float brushRadius, out Vector3 hitPoint)
        {
            hitPoint = Vector3.Zero;

            var closestDistance = float.MaxValue;
            var hasHit = false;

            for (var index = 0; index + 1 < mesh.Indices.Length; index += 2)
            {
                var p0 = mesh.Vertices[(int)mesh.Indices[index]].Position;
                var p1 = mesh.Vertices[(int)mesh.Indices[index + 1]].Position;
                var candidate = ClosestPointOnSegmentToRay(ray, p0, p1, out var rayDistance);
                var distanceToRay = (candidate - (ray.Origin + ray.Direction * rayDistance)).Length;

                if (distanceToRay > brushRadius || rayDistance >= closestDistance)
                {
                    continue;
                }

                closestDistance = rayDistance;
                hitPoint = candidate;
                hasHit = true;
            }

            return hasHit;
        }

        private static bool IntersectsTriangle(Ray ray, Vector3 p0, Vector3 p1, Vector3 p2, out float distance)
        {
            distance = 0f;

            var edge1 = p1 - p0;
            var edge2 = p2 - p0;
            var h = Vector3.Cross(ray.Direction, edge2);
            var determinant = Vector3.Dot(edge1, h);

            if (Math.Abs(determinant) < MathConstants.ZeroTolerance)
            {
                return false;
            }

            var inverseDeterminant = 1f / determinant;
            var s = ray.Origin - p0;
            var u = inverseDeterminant * Vector3.Dot(s, h);

            if (u is < 0f or > 1f)
            {
                return false;
            }

            var q = Vector3.Cross(s, edge1);
            var v = inverseDeterminant * Vector3.Dot(ray.Direction, q);

            if (v < 0f || u + v > 1f)
            {
                return false;
            }

            distance = inverseDeterminant * Vector3.Dot(edge2, q);
            return distance > MathConstants.ZeroTolerance;
        }

        private static Vector3 ClosestPointOnSegmentToRay(Ray ray, Vector3 segmentStart, Vector3 segmentEnd, out float rayDistance)
        {
            var segment = segmentEnd - segmentStart;
            var w0 = ray.Origin - segmentStart;
            var a = Vector3.Dot(ray.Direction, ray.Direction);
            var b = Vector3.Dot(ray.Direction, segment);
            var c = Vector3.Dot(segment, segment);
            var d = Vector3.Dot(ray.Direction, w0);
            var e = Vector3.Dot(segment, w0);
            var denominator = a * c - b * b;

            var segmentParameter = denominator > MathConstants.ZeroTolerance
                ? Math.Clamp((a * e - b * d) / denominator, 0f, 1f)
                : 0f;

            rayDistance = Math.Max(0f, (b * segmentParameter - d) / a);
            return segmentStart + segment * segmentParameter;
        }

        #endregion
    }
}
