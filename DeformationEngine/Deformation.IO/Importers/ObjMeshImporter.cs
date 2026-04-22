using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.IO.Abstractions;
using Deformation.IO.Constants;
using OpenTK.Mathematics;

namespace Deformation.IO.Importers
{
    public sealed class ObjMeshImporter : IMeshImporter
    {
        public string[] SupportedExtensions => [ImporterConstants.Obj.Extension];

        public Mesh Load(Stream stream)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var texCoordinates = new List<Vector2>();

            var vertices = new List<Vertex>();
            var indices = new List<uint>();
            var vertexCache = new Dictionary<string, uint>();

            using var reader = new StreamReader(stream, leaveOpen: true);
            string? line;

            while ((line = reader.ReadLine()) is not null)
            {
                line = line.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                var parts = line.SplitByWhitespace();

                if (parts.Length == 0)
                {
                    continue;
                }

                switch (parts[0])
                {
                    case "v":
                        positions.Add(new Vector3(
                            parts[1].ToFloatInvariant(), 
                            parts[2].ToFloatInvariant(), 
                            parts[3].ToFloatInvariant()));
                        break;

                    case "vn":
                        normals.Add(new Vector3(
                            parts[1].ToFloatInvariant(), 
                            parts[2].ToFloatInvariant(), 
                            parts[3].ToFloatInvariant()));
                        break;

                    case "vt":
                        texCoordinates.Add(new Vector2(
                            parts[1].ToFloatInvariant(), 
                            parts[2].ToFloatInvariant()));
                        break;

                    case "f":
                        ProcessFace(parts);
                        break;
                }
            }

            return new Mesh([.. vertices], [.. indices]);

            void ProcessFace(string[] parts)
            {
                for (var i = 1; i < parts.Length - 2; i++)
                {
                    indices.Add(GetOrAddVertex(parts[1]));
                    indices.Add(GetOrAddVertex(parts[i + 1]));
                    indices.Add(GetOrAddVertex(parts[i + 2]));
                }
            }

            uint GetOrAddVertex(string faceVertexDef)
            {
                if (vertexCache.TryGetValue(faceVertexDef, out var index))
                {
                    return index;
                }

                var indices = faceVertexDef.Split('/');

                var positionIndex = int.Parse(indices[0]);
                var position = positions[positionIndex < 0 ? positions.Count + positionIndex : positionIndex - 1];

                var uv = Vector2.Zero;

                if (indices.Length > 1 && !string.IsNullOrEmpty(indices[1]))
                {
                    var uvIndex = int.Parse(indices[1]);
                    uv = texCoordinates[uvIndex < 0 ? texCoordinates.Count + uvIndex : uvIndex - 1];
                }

                var normal = Vector3.Zero;

                if (indices.Length > 2 && !string.IsNullOrEmpty(indices[2]))
                {
                    var normIndex = int.Parse(indices[2]);
                    normal = normals[normIndex < 0 ? normals.Count + normIndex : normIndex - 1];
                }

                index = (uint)vertices.Count;
                vertices.Add(new Vertex(position, normal, uv));
                vertexCache.Add(faceVertexDef, index);

                return index;
            }
        }
    }
}
