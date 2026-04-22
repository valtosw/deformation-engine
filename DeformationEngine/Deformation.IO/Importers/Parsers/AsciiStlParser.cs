using Deformation.Abstractions.Comparers;
using Deformation.Abstractions.Geometry;
using Deformation.IO.Abstractions;
using OpenTK.Mathematics;
using System.Globalization;
using System.Text;

namespace Deformation.IO.Importers.Parsers
{
    public sealed class AsciiStlParser : IStlParser
    {
        public Mesh Parse(Stream stream)
        {
            using var streamReader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

            var vertices = new List<Vertex>();
            var indices = new List<uint>();
            var vertexCache = new Dictionary<Vector3, uint>(new Vector3EqualityComparer());

            string? line;
            var currentNormal = Vector3.Zero;
            var currentTriangle = new Vector3[3];
            var vertexCount = 0;

            while ((line = streamReader.ReadLine()) is not null)
            {
                var span = line.AsSpan().TrimStart();

                if (span.IsEmpty)
                {
                    continue;
                }

                if (span.StartsWith("vertex", StringComparison.OrdinalIgnoreCase))
                {
                    currentTriangle[vertexCount++] = ParseToVector3(span[6..]);

                    if (vertexCount != 3)
                    {
                        continue;
                    }

                    AddVertex(currentTriangle[0], currentNormal);
                    AddVertex(currentTriangle[1], currentNormal);
                    AddVertex(currentTriangle[2], currentNormal);
                    vertexCount = 0;
                }
                else if (span.StartsWith("facet normal", StringComparison.OrdinalIgnoreCase))
                {
                    currentNormal = ParseToVector3(span[12..]);
                }
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

        private static Vector3 ParseToVector3(ReadOnlySpan<char> span)
        {
            span = span.TrimStart();

            var firstSpace = span.IndexOfAny(' ', '\t');
            var xSpan = span[..firstSpace];

            span = span[(firstSpace + 1)..].TrimStart();

            var secondSpace = span.IndexOfAny(' ', '\t');
            var ySpan = span[..secondSpace];
            var zSpan = span[(secondSpace + 1)..].TrimEnd();

            return new Vector3(
                float.Parse(xSpan, provider: CultureInfo.InvariantCulture),
                float.Parse(ySpan, provider: CultureInfo.InvariantCulture),
                float.Parse(zSpan, provider: CultureInfo.InvariantCulture));
        }
    }
}
