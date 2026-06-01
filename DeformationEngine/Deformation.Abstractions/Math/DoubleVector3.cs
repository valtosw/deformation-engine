namespace Deformation.Abstractions.Math
{
    public readonly struct DoubleVector3(double x, double y, double z)
    {
        public double X { get; } = x;
        public double Y { get; } = y;
        public double Z { get; } = z;

        public static DoubleVector3 operator +(DoubleVector3 left, DoubleVector3 right)
        {
            return new DoubleVector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static DoubleVector3 operator *(double scalar, DoubleVector3 vector)
        {
            return new DoubleVector3(scalar * vector.X, scalar * vector.Y, scalar * vector.Z);
        }
    }
}
