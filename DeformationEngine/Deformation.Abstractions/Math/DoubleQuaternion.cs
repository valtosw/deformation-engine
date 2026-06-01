using Deformation.Abstractions.Constants;

namespace Deformation.Abstractions.Math
{
    public readonly struct DoubleQuaternion(double x, double y, double z, double w)
    {
        public static readonly DoubleQuaternion Identity = new(0d, 0d, 0d, 1d);

        public double X { get; } = x;
        public double Y { get; } = y;
        public double Z { get; } = z;
        public double W { get; } = w;

        public static DoubleQuaternion FromAxisAngle(double axisX, double axisY, double axisZ, double angle)
        {
            var halfAngle = angle * 0.5d;
            var sin = System.Math.Sin(halfAngle);

            return new DoubleQuaternion(
                axisX * sin,
                axisY * sin,
                axisZ * sin,
                System.Math.Cos(halfAngle));
        }

        public DoubleQuaternion Normalized()
        {
            var lengthSquared = X * X + Y * Y + Z * Z + W * W;

            if (lengthSquared < MathConstants.DoublePrecisionTolerance)
            {
                return Identity;
            }

            var inverseLength = 1d / System.Math.Sqrt(lengthSquared);
            return new DoubleQuaternion(X * inverseLength, Y * inverseLength, Z * inverseLength, W * inverseLength);
        }

        public static DoubleQuaternion operator *(DoubleQuaternion left, DoubleQuaternion right)
        {
            return new DoubleQuaternion(
                left.W * right.X + left.X * right.W + left.Y * right.Z - left.Z * right.Y,
                left.W * right.Y - left.X * right.Z + left.Y * right.W + left.Z * right.X,
                left.W * right.Z + left.X * right.Y - left.Y * right.X + left.Z * right.W,
                left.W * right.W - left.X * right.X - left.Y * right.Y - left.Z * right.Z);
        }
    }
}
