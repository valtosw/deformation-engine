using Deformation.Abstractions.Constants;
using OpenTK.Mathematics;

namespace Deformation.Abstractions.Extensions
{
    public static class MatrixExtensions
    {
        public static Vector3 TransformPoint(this Matrix4 matrix, Vector3 point)
        {
            var v4 = new Vector4(point, 1f) * matrix;
            return v4.Xyz / v4.W;
        }

        public static Vector3 TransformDirection(this Matrix4 matrix, Vector3 direction)
        {
            var v4 = new Vector4(direction, 0f) * matrix;
            return v4.Xyz;
        }

        public static bool IsClose(this Matrix4 left, Matrix4 right)
        {
            return
                (left.Row0 - right.Row0).LengthSquared <= MathConstants.ZeroTolerance &&
                (left.Row1 - right.Row1).LengthSquared <= MathConstants.ZeroTolerance &&
                (left.Row2 - right.Row2).LengthSquared <= MathConstants.ZeroTolerance &&
                (left.Row3 - right.Row3).LengthSquared <= MathConstants.ZeroTolerance;
        }

        public static Matrix4 GetNormalMatrix(this Matrix4 matrix)
        {
            var normalMatrix = matrix;
            normalMatrix.Invert();
            normalMatrix.Transpose();

            return normalMatrix;
        }
    }
}
