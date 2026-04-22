namespace Deformation.Abstractions.Geometry
{
    public sealed class Mesh
    {
        public Vertex[] Vertices { get; set; }
        public uint[] Indices { get; set; }
        public AxisAlignedBoundingBox LocalBoundingBox { get; set; }

        public Mesh(Vertex[] vertices, uint[] indicies)
        {
            Vertices = vertices;
            Indices = indicies;
            LocalBoundingBox = AxisAlignedBoundingBox.FromPoints(Vertices.Select(v => v.Position));
        }

        public static Mesh FromTriangles(List<Triangle> triangles)
        {
            var vertices = new Vertex[triangles.Count * 3];
            var indices = new uint[triangles.Count * 3];

            for (var i = 0; i < triangles.Count; i++)
            {
                var triangle = triangles[i];

                vertices[i * 3 + 0] = new Vertex(triangle.Vertex0, triangle.Normal);
                vertices[i * 3 + 1] = new Vertex(triangle.Vertex1, triangle.Normal);
                vertices[i * 3 + 2] = new Vertex(triangle.Vertex2, triangle.Normal);

                indices[i * 3 + 0] = (uint)(i * 3 + 0);
                indices[i * 3 + 1] = (uint)(i * 3 + 1);
                indices[i * 3 + 2] = (uint)(i * 3 + 2);
            }

            return new Mesh(vertices, indices);
        }
    }
}
