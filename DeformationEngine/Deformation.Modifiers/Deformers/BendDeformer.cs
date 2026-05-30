using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers
{
    public sealed class BendDeformer : IDeformer
    {
        #region Properties

        public float Angle { get; set; }
        public Axis PrimaryAxis { get; set; } = Axis.Y;
        public Axis BendAxis { get; set; } = Axis.X;
        public float Pivot { get; set; } = 0.5f;
        public bool PreventSelfIntersection { get; set; } = true;

        #endregion

        #region Public Logic

        public void Deform(Mesh mesh)
        {
            if (MathF.Abs(Angle) < MathConstants.ZeroTolerance || PrimaryAxis == BendAxis)
            {
                return;
            }

            mesh.CalculateBounds(out var min, out var max);
            Deform(mesh.Vertices, min, max);
        }

        public void Deform(Span<Vertex> vertices, Vector3 min, Vector3 max)
        {
            if (MathF.Abs(Angle) < MathConstants.ZeroTolerance || PrimaryAxis == BendAxis)
            {
                return;
            }

            var primaryIndex = (int)PrimaryAxis;
            var bendIndex = (int)BendAxis;

            var length = max[primaryIndex] - min[primaryIndex];

            if (length < MathConstants.LengthTolerance)
            {
                return;
            }

            var pivotCoord = min[primaryIndex] + length * Pivot;
            var bendCenter = (min[bendIndex] + max[bendIndex]) * 0.5f;

            var effectiveAngle = Angle;

            if (PreventSelfIntersection)
            {
                var halfWidth = (max[bendIndex] - min[bendIndex]) * 0.5f;

                if (halfWidth > MathConstants.LengthTolerance)
                {
                    var maxSafeAngle = length / halfWidth;
                    effectiveAngle = Math.Clamp(effectiveAngle, -maxSafeAngle, maxSafeAngle);
                }
            }

            var radius = length / effectiveAngle;

            for (var index = 0; index < vertices.Length; index++)
            {
                var position = vertices[index].Position;
                var normal = vertices[index].Normal;

                var relativePrimary = position[primaryIndex] - pivotCoord;
                var theta = effectiveAngle * (relativePrimary / length);

                var cosTheta = MathF.Cos(theta);
                var sinTheta = MathF.Sin(theta);

                var localBendAxis = position[bendIndex] - bendCenter;
                var distanceToCenter = radius - localBendAxis;

                var newBend = radius - distanceToCenter * cosTheta;
                var newPrimary = distanceToCenter * sinTheta;

                var newNormalBend = normal[bendIndex] * cosTheta - normal[primaryIndex] * sinTheta;
                var newNormalPrimary = normal[bendIndex] * sinTheta + normal[primaryIndex] * cosTheta;

                position[primaryIndex] = newPrimary + pivotCoord;
                position[bendIndex] = newBend + bendCenter;

                normal[primaryIndex] = newNormalPrimary;
                normal[bendIndex] = newNormalBend;

                vertices[index].Position = position;
                vertices[index].Normal = normal.LengthSquared > MathConstants.LengthTolerance ? normal.Normalized() : normal;
            }
        }

        #endregion
    }
}