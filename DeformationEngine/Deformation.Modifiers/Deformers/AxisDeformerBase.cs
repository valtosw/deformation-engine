using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers
{
    public abstract class AxisDeformerBase : IDeformer
    {
        #region Properties

        public float Angle { get; set; }
        public float Pivot { get; set; } = 0.5f;
        public bool PreventSelfIntersection { get; set; } = true;

        #endregion

        #region Public Logic

        public void Deform(Mesh mesh)
        {
            if (MathF.Abs(Angle) < MathConstants.ZeroTolerance || !IsValidSetup())
            {
                return;
            }

            mesh.CalculateBounds(out var min, out var max);
            Deform(mesh.Vertices, min, max);
        }

        public void Deform(Span<Vertex> vertices, Vector3 min, Vector3 max)
        {
            if (MathF.Abs(Angle) < MathConstants.ZeroTolerance || !IsValidSetup())
            {
                return;
            }

            var length = CalculateLength(min, max);

            if (length < MathConstants.LengthTolerance)
            {
                return;
            }

            var effectiveAngle = Angle;

            if (PreventSelfIntersection)
            {
                effectiveAngle = ClampAngleForSelfIntersection(effectiveAngle, length, min, max);
            }

            PrepareDeformation(min, max, length, effectiveAngle);

            for (var index = 0; index < vertices.Length; index++)
            {
                vertices[index] = DeformVertex(vertices[index], min, max, effectiveAngle, length);
            }
        }

        #endregion

        #region Protected Logic

        protected virtual bool IsValidSetup()
        {
            return true;
        }

        protected virtual void PrepareDeformation(Vector3 min, Vector3 max, float length, float effectiveAngle) { }

        protected abstract float CalculateLength(Vector3 min, Vector3 max);

        protected abstract float ClampAngleForSelfIntersection(float angle, float length, Vector3 min, Vector3 max);

        protected abstract Vertex DeformVertex(Vertex vertex, Vector3 min, Vector3 max, float effectiveAngle, float length);

        #endregion
    }
}