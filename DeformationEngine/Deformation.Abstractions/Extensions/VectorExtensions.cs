using Deformation.Abstractions.Constants;
using OpenTK.Mathematics;

namespace Deformation.Abstractions.Extensions
{
    public static class VectorExtensions
    {
        public static Vector3 TransformNormal(this Vector3 normal, Vector3 derivativeS, Vector3 derivativeT, Vector3 derivativeU, Vector3 latticeSize)
        {
            if (normal.LengthSquared < MathConstants.LengthTolerance)
            {
                return normal;
            }

            var normalizedNormal = normal.Normalized();
            var derivativeX = latticeSize.X > MathConstants.LengthTolerance ? derivativeS / latticeSize.X : Vector3.UnitX;
            var derivativeY = latticeSize.Y > MathConstants.LengthTolerance ? derivativeT / latticeSize.Y : Vector3.UnitY;
            var derivativeZ = latticeSize.Z > MathConstants.LengthTolerance ? derivativeU / latticeSize.Z : Vector3.UnitZ;

            var referenceAxis = MathF.Abs(Vector3.Dot(normalizedNormal, Vector3.UnitY)) < 0.95f
                ? Vector3.UnitY
                : Vector3.UnitX;

            var tangent = Vector3.Cross(referenceAxis, normalizedNormal);

            if (tangent.LengthSquared < MathConstants.LengthTolerance)
            {
                return normalizedNormal;
            }

            tangent.Normalize();

            var bitangent = Vector3.Cross(normalizedNormal, tangent);

            if (bitangent.LengthSquared < MathConstants.LengthTolerance)
            {
                return normalizedNormal;
            }

            bitangent.Normalize();

            var transformedTangent = tangent.TransformByGradient(derivativeX, derivativeY, derivativeZ);
            var transformedBitangent = bitangent.TransformByGradient(derivativeX, derivativeY, derivativeZ);
            var transformedNormal = Vector3.Cross(transformedTangent, transformedBitangent);

            return transformedNormal.LengthSquared > MathConstants.LengthTolerance
                ? transformedNormal.Normalized()
                : normalizedNormal;
        }

        public static Vector3 TransformByGradient(this Vector3 direction, Vector3 derivativeX, Vector3 derivativeY, Vector3 derivativeZ)
        {
            return derivativeX * direction.X + derivativeY * direction.Y + derivativeZ * direction.Z;
        }
    }
}
