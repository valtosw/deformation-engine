using OpenTK.Mathematics;

namespace Visualization.Abstractions.Geometry
{
    public record struct Triangle(Vector3 Vertex0, Vector3 Vertex1, Vector3 Vertex2)
    {
        public Vector3 Normal { get; } = Vector3.Normalize(Vector3.Cross(Vertex1 - Vertex0, Vertex2 - Vertex0));

        public IEnumerable<Vector3> Vertices
        {
            get
            {
                yield return Vertex0;
                yield return Vertex1;
                yield return Vertex2;
            }
        }
    }
}
