using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;

namespace Deformation.Abstractions.Extensions
{
    public static class MeshExtensions
    {
        public static void CalculateBounds(this Mesh mesh, out Vector3 min, out Vector3 max)
        {
            min = new Vector3(float.MaxValue);
            max = new Vector3(float.MinValue);

            for (var index = 0; index < mesh.Vertices.Length; index++)
            {
                var position = mesh.Vertices[index].Position;
                min = Vector3.ComponentMin(min, position);
                max = Vector3.ComponentMax(max, position);
            }
        }
    }
}
