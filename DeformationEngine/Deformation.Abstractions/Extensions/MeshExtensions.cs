using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;

namespace Deformation.Abstractions.Extensions
{
    public static class MeshExtensions
    {
        #region Public Logic

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

        public static Mesh Subdivide(this Mesh mesh)
        {
            if (mesh.Topology != MeshTopology.Triangles)
            {
                return mesh;
            }

            var newVertices = new List<Vertex>(mesh.Vertices);
            var newIndices = new List<uint>();
            var midpointCache = new Dictionary<EdgeKey, uint>();

            for (var index = 0; index < mesh.Indices.Length; index += 3)
            {
                var index0 = mesh.Indices[index];
                var index1 = mesh.Indices[index + 1];
                var index2 = mesh.Indices[index + 2];

                var midpoint01 = GetMidpoint(index0, index1);
                var midpoint12 = GetMidpoint(index1, index2);
                var midpoint20 = GetMidpoint(index2, index0);

                newIndices.AddRange([index0, midpoint01, midpoint20]);
                newIndices.AddRange([index1, midpoint12, midpoint01]);
                newIndices.AddRange([index2, midpoint20, midpoint12]);
                newIndices.AddRange([midpoint01, midpoint12, midpoint20]);
            }

            return new Mesh([.. newVertices], [.. newIndices])
            {
                Topology = mesh.Topology
            };

            uint GetMidpoint(uint indexA, uint indexB)
            {
                var vertexA = mesh.Vertices[indexA];
                var vertexB = mesh.Vertices[indexB];
                var key = new EdgeKey(vertexA.Position, vertexB.Position);

                if (midpointCache.TryGetValue(key, out var midpointIndex))
                {
                    return midpointIndex;
                }

                var midPosition = (vertexA.Position + vertexB.Position) * 0.5f;
                var midNormal = (vertexA.Normal + vertexB.Normal).Normalized();
                var midTexCoord = (vertexA.TexCoords + vertexB.TexCoords) * 0.5f;

                midpointIndex = (uint)newVertices.Count;
                newVertices.Add(new Vertex(midPosition, midNormal, midTexCoord));
                midpointCache.Add(key, midpointIndex);

                return midpointIndex;
            }
        }

        #endregion
    }
}