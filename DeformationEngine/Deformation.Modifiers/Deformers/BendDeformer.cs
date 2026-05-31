using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers
{
    public sealed class BendDeformer : AxisDeformerBase
    {
        #region Fields

        private int _primaryIndex;
        private int _bendIndex;
        private float _pivotCoord;
        private float _bendCenter;
        private float _radius;

        #endregion

        #region Properties

        public Axis PrimaryAxis { get; set; } = Axis.Y;
        public Axis BendAxis { get; set; } = Axis.X;

        #endregion

        #region Protected Logic

        protected override bool IsValidSetup()
        {
            return PrimaryAxis != BendAxis;
        }

        protected override float CalculateLength(Vector3 min, Vector3 max)
        {
            return max[(int)PrimaryAxis] - min[(int)PrimaryAxis];
        }

        protected override float ClampAngleForSelfIntersection(float angle, float length, Vector3 min, Vector3 max)
        {
            var bendIndex = (int)BendAxis;
            var halfWidth = (max[bendIndex] - min[bendIndex]) * 0.5f;

            if (halfWidth > MathConstants.LengthTolerance)
            {
                var maxSafeAngle = length / halfWidth;
                return Math.Clamp(angle, -maxSafeAngle, maxSafeAngle);
            }

            return angle;
        }

        protected override void PrepareDeformation(Vector3 min, Vector3 max, float length, float effectiveAngle)
        {
            _primaryIndex = (int)PrimaryAxis;
            _bendIndex = (int)BendAxis;

            _pivotCoord = min[_primaryIndex] + length * Pivot;
            _bendCenter = (min[_bendIndex] + max[_bendIndex]) * 0.5f;

            _radius = length / effectiveAngle;
        }

        protected override Vertex DeformVertex(Vertex vertex, Vector3 min, Vector3 max, float effectiveAngle, float length)
        {
            var position = vertex.Position;
            var normal = vertex.Normal;

            var relativePrimary = position[_primaryIndex] - _pivotCoord;
            var theta = effectiveAngle * (relativePrimary / length);

            var cosTheta = MathF.Cos(theta);
            var sinTheta = MathF.Sin(theta);

            var localBendAxis = position[_bendIndex] - _bendCenter;
            var distanceToCenter = _radius - localBendAxis;

            var newBend = _radius - distanceToCenter * cosTheta;
            var newPrimary = distanceToCenter * sinTheta;

            var newNormalBend = normal[_bendIndex] * cosTheta - normal[_primaryIndex] * sinTheta;
            var newNormalPrimary = normal[_bendIndex] * sinTheta + normal[_primaryIndex] * cosTheta;

            position[_primaryIndex] = newPrimary + _pivotCoord;
            position[_bendIndex] = newBend + _bendCenter;

            normal[_primaryIndex] = newNormalPrimary;
            normal[_bendIndex] = newNormalBend;

            var finalNormal = normal.LengthSquared > MathConstants.LengthTolerance ? normal.Normalized() : normal;

            return new Vertex(position, finalNormal, vertex.TexCoords);
        }

        #endregion
    }
}