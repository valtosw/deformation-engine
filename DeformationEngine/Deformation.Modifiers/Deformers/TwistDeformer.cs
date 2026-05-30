using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers
{
    public sealed class TwistDeformer : IDeformer
    {
        #region Properties

        public float Angle { get; set; }
        public Axis Axis { get; set; } = Axis.Y;
        public float Pivot { get; set; } = 0.5f;
        public bool PreventSelfIntersection { get; set; } = true;

        #endregion

        #region Public Logic

        public void Deform(Mesh mesh)
        {
            if (MathF.Abs(Angle) < MathConstants.ZeroTolerance)
            {
                return;
            }

            mesh.CalculateBounds(out var min, out var max);
            Deform(mesh.Vertices, min, max);
        }

        public void Deform(Span<Vertex> vertices, Vector3 min, Vector3 max)
        {
            if (MathF.Abs(Angle) < MathConstants.ZeroTolerance)
            {
                return;
            }

            var axisIndex = (int)Axis;
            var axis1 = (axisIndex + 1) % 3;
            var axis2 = (axisIndex + 2) % 3;

            var length = max[axisIndex] - min[axisIndex];

            if (length < MathConstants.LengthTolerance)
            {
                return;
            }

            var pivotCoord = min[axisIndex] + length * Pivot;
            var centerAxis1 = (min[axis1] + max[axis1]) * 0.5f;
            var centerAxis2 = (min[axis2] + max[axis2]) * 0.5f;

            var effectiveAngle = Angle;

            if (PreventSelfIntersection)
            {
                var width1 = max[axis1] - min[axis1];
                var width2 = max[axis2] - min[axis2];
                var maxWidth = MathF.Max(width1, width2);

                if (maxWidth > MathConstants.LengthTolerance)
                {
                    var maxSafeAngle = MathF.PI * (length / maxWidth);
                    effectiveAngle = Math.Clamp(effectiveAngle, -maxSafeAngle, maxSafeAngle);
                }
            }

            for (var index = 0; index < vertices.Length; index++)
            {
                var position = vertices[index].Position;
                var normal = vertices[index].Normal;

                var relativePosition = (position[axisIndex] - pivotCoord) / length;
                var theta = effectiveAngle * relativePosition;

                var cosTheta = MathF.Cos(theta);
                var sinTheta = MathF.Sin(theta);

                var localAxis1 = position[axis1] - centerAxis1;
                var localAxis2 = position[axis2] - centerAxis2;

                var newAxis1 = localAxis1 * cosTheta - localAxis2 * sinTheta;
                var newAxis2 = localAxis1 * sinTheta + localAxis2 * cosTheta;

                var newNormalAxis1 = normal[axis1] * cosTheta - normal[axis2] * sinTheta;
                var newNormalAxis2 = normal[axis1] * sinTheta + normal[axis2] * cosTheta;

                position[axis1] = newAxis1 + centerAxis1;
                position[axis2] = newAxis2 + centerAxis2;

                normal[axis1] = newNormalAxis1;
                normal[axis2] = newNormalAxis2;

                vertices[index].Position = position;
                vertices[index].Normal = normal.LengthSquared > MathConstants.LengthTolerance ? normal.Normalized() : normal;
            }
        }

        #endregion
    }
}