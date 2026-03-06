using OpenTK.Mathematics;

namespace Visualization.Abstractions.Extensions
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
    }
}
