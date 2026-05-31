using System;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers
{
    public sealed class TwistDeformer : AxisDeformerBase
    {
        #region Fields

        private int _axisIndex;
        private int _axis1;
        private int _axis2;
        private float _pivotCoord;
        private float _centerAxis1;
        private float _centerAxis2;

        #endregion

        #region Properties

        public Axis Axis { get; set; } = Axis.Y;

        #endregion

        #region Protected Logic

        protected override float CalculateLength(Vector3 min, Vector3 max)
        {
            return max[(int)Axis] - min[(int)Axis];
        }

        protected override float ClampAngleForSelfIntersection(float angle, float length, Vector3 min, Vector3 max)
        {
            var axisIndex = (int)Axis;
            var axis1 = (axisIndex + 1) % 3;
            var axis2 = (axisIndex + 2) % 3;

            var width1 = max[axis1] - min[axis1];
            var width2 = max[axis2] - min[axis2];
            var maxWidth = MathF.Max(width1, width2);

            if (maxWidth > MathConstants.LengthTolerance)
            {
                var maxSafeAngle = MathConstants.Pi * (length / maxWidth);
                return Math.Clamp(angle, -maxSafeAngle, maxSafeAngle);
            }

            return angle;
        }

        protected override void PrepareDeformation(Vector3 min, Vector3 max, float length, float effectiveAngle)
        {
            _axisIndex = (int)Axis;
            _axis1 = (_axisIndex + 1) % 3;
            _axis2 = (_axisIndex + 2) % 3;

            _pivotCoord = min[_axisIndex] + length * Pivot;
            _centerAxis1 = (min[_axis1] + max[_axis1]) * 0.5f;
            _centerAxis2 = (min[_axis2] + max[_axis2]) * 0.5f;
        }

        protected override Vertex DeformVertex(Vertex vertex, Vector3 min, Vector3 max, float effectiveAngle, float length)
        {
            var position = vertex.Position;
            var normal = vertex.Normal;

            var relativePosition = (position[_axisIndex] - _pivotCoord) / length;
            var theta = effectiveAngle * relativePosition;

            var cosTheta = MathF.Cos(theta);
            var sinTheta = MathF.Sin(theta);

            var localAxis1 = position[_axis1] - _centerAxis1;
            var localAxis2 = position[_axis2] - _centerAxis2;

            var newAxis1 = localAxis1 * cosTheta - localAxis2 * sinTheta;
            var newAxis2 = localAxis1 * sinTheta + localAxis2 * cosTheta;

            var newNormalAxis1 = normal[_axis1] * cosTheta - normal[_axis2] * sinTheta;
            var newNormalAxis2 = normal[_axis1] * sinTheta + normal[_axis2] * cosTheta;

            position[_axis1] = newAxis1 + _centerAxis1;
            position[_axis2] = newAxis2 + _centerAxis2;

            normal[_axis1] = newNormalAxis1;
            normal[_axis2] = newNormalAxis2;

            var finalNormal = normal.LengthSquared > MathConstants.LengthTolerance ? normal.Normalized() : normal;

            return new Vertex(position, finalNormal, vertex.TexCoords);
        }

        #endregion
    }
}