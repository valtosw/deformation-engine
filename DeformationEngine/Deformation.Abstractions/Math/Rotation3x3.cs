using OpenTK.Mathematics;

namespace Deformation.Abstractions.Math
{
    public readonly struct Rotation3x3(
        double m11,
        double m12,
        double m13,
        double m21,
        double m22,
        double m23,
        double m31,
        double m32,
        double m33)
    {
        private const int PolarRotationIterations = 8;

        public static readonly Rotation3x3 Identity = new(
            1d,
            0d,
            0d,
            0d,
            1d,
            0d,
            0d,
            0d,
            1d);

        public double M11 { get; } = m11;
        public double M12 { get; } = m12;
        public double M13 { get; } = m13;
        public double M21 { get; } = m21;
        public double M22 { get; } = m22;
        public double M23 { get; } = m23;
        public double M31 { get; } = m31;
        public double M32 { get; } = m32;
        public double M33 { get; } = m33;

        public static Rotation3x3 FromQuaternion(DoubleQuaternion quaternion)
        {
            var xx = quaternion.X * quaternion.X;
            var yy = quaternion.Y * quaternion.Y;
            var zz = quaternion.Z * quaternion.Z;
            var xy = quaternion.X * quaternion.Y;
            var xz = quaternion.X * quaternion.Z;
            var yz = quaternion.Y * quaternion.Z;
            var wx = quaternion.W * quaternion.X;
            var wy = quaternion.W * quaternion.Y;
            var wz = quaternion.W * quaternion.Z;

            return new Rotation3x3(
                1d - 2d * (yy + zz),
                2d * (xy - wz),
                2d * (xz + wy),
                2d * (xy + wz),
                1d - 2d * (xx + zz),
                2d * (yz - wx),
                2d * (xz - wy),
                2d * (yz + wx),
                1d - 2d * (xx + yy));
        }

        public static Rotation3x3 FromCovariance(
            double c11,
            double c12,
            double c13,
            double c21,
            double c22,
            double c23,
            double c31,
            double c32,
            double c33)
        {
            var covarianceMagnitude =
                System.Math.Abs(c11) + System.Math.Abs(c12) + System.Math.Abs(c13) +
                System.Math.Abs(c21) + System.Math.Abs(c22) + System.Math.Abs(c23) +
                System.Math.Abs(c31) + System.Math.Abs(c32) + System.Math.Abs(c33);

            if (covarianceMagnitude < 1e-12d)
            {
                return Identity;
            }

            var rotation = DoubleQuaternion.Identity;

            for (var iteration = 0; iteration < PolarRotationIterations; iteration++)
            {
                var matrix = FromQuaternion(rotation);

                var omegaX =
                    CrossX(matrix.M11, matrix.M21, matrix.M31, c11, c21, c31) +
                    CrossX(matrix.M12, matrix.M22, matrix.M32, c12, c22, c32) +
                    CrossX(matrix.M13, matrix.M23, matrix.M33, c13, c23, c33);
                var omegaY =
                    CrossY(matrix.M11, matrix.M21, matrix.M31, c11, c21, c31) +
                    CrossY(matrix.M12, matrix.M22, matrix.M32, c12, c22, c32) +
                    CrossY(matrix.M13, matrix.M23, matrix.M33, c13, c23, c33);
                var omegaZ =
                    CrossZ(matrix.M11, matrix.M21, matrix.M31, c11, c21, c31) +
                    CrossZ(matrix.M12, matrix.M22, matrix.M32, c12, c22, c32) +
                    CrossZ(matrix.M13, matrix.M23, matrix.M33, c13, c23, c33);

                var denominator = System.Math.Abs(
                    Dot(matrix.M11, matrix.M21, matrix.M31, c11, c21, c31) +
                    Dot(matrix.M12, matrix.M22, matrix.M32, c12, c22, c32) +
                    Dot(matrix.M13, matrix.M23, matrix.M33, c13, c23, c33)) + 1e-12d;

                omegaX /= denominator;
                omegaY /= denominator;
                omegaZ /= denominator;

                var omegaLength = System.Math.Sqrt(omegaX * omegaX + omegaY * omegaY + omegaZ * omegaZ);

                if (omegaLength < 1e-9d)
                {
                    break;
                }

                var correction = DoubleQuaternion.FromAxisAngle(
                    omegaX / omegaLength,
                    omegaY / omegaLength,
                    omegaZ / omegaLength,
                    omegaLength);

                rotation = (correction * rotation).Normalized();
            }

            return FromQuaternion(rotation);
        }

        public DoubleVector3 Transform(Vector3 vector)
        {
            return new DoubleVector3(
                M11 * vector.X + M12 * vector.Y + M13 * vector.Z,
                M21 * vector.X + M22 * vector.Y + M23 * vector.Z,
                M31 * vector.X + M32 * vector.Y + M33 * vector.Z);
        }

        private static double Dot(double ax, double ay, double az, double bx, double by, double bz)
        {
            return ax * bx + ay * by + az * bz;
        }

        private static double CrossX(double ax, double ay, double az, double bx, double by, double bz)
        {
            return ay * bz - az * by;
        }

        private static double CrossY(double ax, double ay, double az, double bx, double by, double bz)
        {
            return az * bx - ax * bz;
        }

        private static double CrossZ(double ax, double ay, double az, double bx, double by, double bz)
        {
            return ax * by - ay * bx;
        }
    }
}
