using Deformation.Abstractions.Constants;
using OpenTK.Mathematics;

namespace Deformation.Abstractions.Geometry
{
    public readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        public readonly Vector3 A;
        public readonly Vector3 B;

        public EdgeKey(Vector3 a, Vector3 b)
        {
            if (a.X < b.X ||
               (System.Math.Abs(a.X - b.X) < MathConstants.ZeroTolerance && a.Y < b.Y) ||
               (System.Math.Abs(a.X - b.X) < MathConstants.ZeroTolerance && System.Math.Abs(a.Y - b.Y) < MathConstants.ZeroTolerance && a.Z < b.Z))
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public bool Equals(EdgeKey other)
        {
            return A.Equals(other.A) && B.Equals(other.B);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(A, B);
        }

        public override bool Equals(object? obj)
        {
            return obj is EdgeKey key && Equals(key);
        }

        public static bool operator ==(EdgeKey left, EdgeKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EdgeKey left, EdgeKey right)
        {
            return !(left == right);
        }
    }
}
