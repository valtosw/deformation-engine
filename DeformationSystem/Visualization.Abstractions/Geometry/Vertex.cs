using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace Visualization.Abstractions.Geometry
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vertex(Vector3 position, Vector3 normal = default, Vector2 texCoords = default)
    {
        public Vector3 Position = position;
        public Vector3 Normal = normal;
        public Vector2 TexCoords = texCoords;
    }
}
