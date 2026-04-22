using System.Text;
using FileProcessing.Abstractions;
using FileProcessing.Constants;
using OpenTK.Mathematics;
using Visualization.Abstractions.Comparers;
using Visualization.Abstractions.Geometry;

namespace FileProcessing.Importers.Parsers
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
            var vertexCache = new Dictionary<Vector3, uint>(new Vector3EqualityComparer());

            for (var i = 0; i < triangleCount; i++)
            {
                var normal = ReadVector3(binaryReader);

                AddVertex(ReadVector3(binaryReader), normal);
                AddVertex(ReadVector3(binaryReader), normal);
                AddVertex(ReadVector3(binaryReader), normal);

                binaryReader.ReadUInt16();
            }

            return new Mesh([.. vertices], [.. indices]);

            void AddVertex(Vector3 position, Vector3 normal)
            {
                if (!vertexCache.TryGetValue(position, out var index))
                {
                    index = (uint)vertices.Count;
                    vertices.Add(new Vertex(position, normal));
                    vertexCache[position] = index;
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
