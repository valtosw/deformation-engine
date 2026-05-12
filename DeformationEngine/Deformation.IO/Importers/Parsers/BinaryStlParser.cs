using System.Text;
using Deformation.Abstractions.Comparers;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Geometry;
using Deformation.IO.Abstractions;
using Deformation.IO.Constants;
using OpenTK.Mathematics;

namespace Deformation.IO.Importers.Parsers
{
    public sealed class BinaryStlParser : IStlParser
    {
        public Mesh Parse(Stream stream)
        {
            using var binaryReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            binaryReader.ReadBytes(ImporterConstants.Stl.HeaderSize);

            var triangleCount = (int)binaryReader.ReadUInt32();
            var vertices = new List<Vertex>(triangleCount * ImporterConstants.Stl.VerticesPerTriangle / 2);
            var indices = new List<uint>(triangleCount * ImporterConstants.Stl.VerticesPerTriangle);
            var normalSums = new List<Vector3>(vertices.Capacity);
            var vertexCache = new Dictionary<Vector3, uint>(new Vector3EqualityComparer());

            for (var i = 0; i < triangleCount; i++)
            {
                var normal = ReadVector3(binaryReader);

                AddVertex(ReadVector3(binaryReader), normal);
                AddVertex(ReadVector3(binaryReader), normal);
                AddVertex(ReadVector3(binaryReader), normal);

                binaryReader.ReadUInt16();
            }

            for (var i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                var sum = normalSums[i];
                vertex.Normal = sum.LengthSquared > MathConstants.LengthTolerance ? sum.Normalized() : Vector3.UnitZ;
                vertices[i] = vertex;
            }

            return new Mesh([.. vertices], [.. indices]);

            void AddVertex(Vector3 position, Vector3 normal)
            {
                if (!vertexCache.TryGetValue(position, out var index))
                {
                    index = (uint)vertices.Count;
                    vertices.Add(new Vertex(position));
                    normalSums.Add(normal);
                    vertexCache[position] = index;
                }
                else
                {
                    normalSums[(int)index] += normal;
                }

                indices.Add(index);
            }
        }

        private static Vector3 ReadVector3(BinaryReader reader)
        {
            return new Vector3(
                reader.ReadSingle(), 
                reader.ReadSingle(), 
                reader.ReadSingle()
            );
        }
    }
}
