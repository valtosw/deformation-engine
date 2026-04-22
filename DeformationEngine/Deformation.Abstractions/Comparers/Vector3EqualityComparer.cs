using Deformation.Abstractions.Constants;
using OpenTK.Mathematics;

namespace Deformation.Abstractions.Comparers
{
    public sealed class Vector3EqualityComparer : IEqualityComparer<Vector3>
    {
        public bool Equals(Vector3 a, Vector3 b)
        {
            return System.Math.Abs(a.X - b.X) < MathConstants.LengthTolerance &&
                   System.Math.Abs(a.Y - b.Y) < MathConstants.LengthTolerance &&
                   System.Math.Abs(a.Z - b.Z) < MathConstants.LengthTolerance;
        }

        public int GetHashCode(Vector3 vector)
        {
            var qx = System.Math.Round(vector.X / MathConstants.LengthTolerance);
            var qy = System.Math.Round(vector.Y / MathConstants.LengthTolerance);
            var qz = System.Math.Round(vector.Z / MathConstants.LengthTolerance);

            return HashCode.Combine(qx, qy, qz);
        }
    }
}
