using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;

namespace Deformation.Abstractions.Extensions
{
    public static class MeshNormalExtensions
    {
        public static void RecalculateNormals(this Mesh mesh)
        {
            if (mesh.Topology != MeshTopology.Triangles)
            {
                return;
            }

            var normals = new Vector3[mesh.Vertices.Length];

            for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
            {
                var index0 = (int)mesh.Indices[index];
                var index1 = (int)mesh.Indices[index + 1];
                var index2 = (int)mesh.Indices[index + 2];

                var p0 = mesh.Vertices[index0].Position;
                var p1 = mesh.Vertices[index1].Position;
                var p2 = mesh.Vertices[index2].Position;

                var normal = Vector3.Cross(p1 - p0, p2 - p0);

                if (normal.LengthSquared < MathConstants.LengthTolerance)
                {
                    continue;
                }

                normals[index0] += normal;
                normals[index1] += normal;
                normals[index2] += normal;
            }

            for (var index = 0; index < mesh.Vertices.Length; index++)
            {
                var normal = normals[index].LengthSquared > MathConstants.LengthTolerance
                    ? normals[index].Normalized()
                    : mesh.Vertices[index].Normal;

                mesh.Vertices[index] = new Vertex(mesh.Vertices[index].Position, normal, mesh.Vertices[index].TexCoords);
            }
        }
    }
}
