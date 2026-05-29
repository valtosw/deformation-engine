using OpenTK.Mathematics;

namespace Deformation.Abstractions.Geometry
{
    public static class MeshFactory
    {
        #region Public Logic

        public static Mesh CreateBox(Vector3 size, Vector3 center)
        {
            var halfSize = size * 0.5f;
            var min = center - halfSize;
            var max = center + halfSize;

            var vertices = new Vector3[]
            {
                new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z), new(max.X, max.Y, max.Z), new(min.X, max.Y, max.Z),
                new(max.X, min.Y, min.Z), new(min.X, min.Y, min.Z), new(min.X, max.Y, min.Z), new(max.X, max.Y, min.Z),
                new(min.X, min.Y, min.Z), new(min.X, min.Y, max.Z), new(min.X, max.Y, max.Z), new(min.X, max.Y, min.Z),
                new(max.X, min.Y, max.Z), new(max.X, min.Y, min.Z), new(max.X, max.Y, min.Z), new(max.X, max.Y, max.Z),
                new(min.X, max.Y, max.Z), new(max.X, max.Y, max.Z), new(max.X, max.Y, min.Z), new(min.X, max.Y, min.Z),
                new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z), new(max.X, min.Y, max.Z), new(min.X, min.Y, max.Z)
            };

            var normals = new Vector3[]
            {
                Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ,
                -Vector3.UnitZ, -Vector3.UnitZ, -Vector3.UnitZ, -Vector3.UnitZ,
                -Vector3.UnitX, -Vector3.UnitX, -Vector3.UnitX, -Vector3.UnitX,
                Vector3.UnitX, Vector3.UnitX, Vector3.UnitX, Vector3.UnitX,
                Vector3.UnitY, Vector3.UnitY, Vector3.UnitY, Vector3.UnitY,
                -Vector3.UnitY, -Vector3.UnitY, -Vector3.UnitY, -Vector3.UnitY
            };

            var meshVertices = new Vertex[24];

            for (var index = 0; index < 24; index++)
            {
                meshVertices[index] = new Vertex(vertices[index], normals[index]);
            }

            var indices = new uint[]
            {
                0, 1, 2, 2, 3, 0,
                4, 5, 6, 6, 7, 4,
                8, 9, 10, 10, 11, 8,
                12, 13, 14, 14, 15, 12,
                16, 17, 18, 18, 19, 16,
                20, 21, 22, 22, 23, 20
            };

            return new Mesh(meshVertices, indices);
        }

        public static Mesh CreateCylinder(float radius, float height, int segments, Vector3 offset)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            var yBottom = offset.Y;
            var yTop = offset.Y + height;

            var bottomCenterIndex = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(offset.X, yBottom, offset.Z), -Vector3.UnitY));

            var topCenterIndex = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(offset.X, yTop, offset.Z), Vector3.UnitY));

            var startIndex = (uint)vertices.Count;

            for (var index = 0; index <= segments; index++)
            {
                var theta = (float)index / segments * MathHelper.TwoPi;
                var cos = MathF.Cos(theta);
                var sin = MathF.Sin(theta);

                var x = offset.X + radius * cos;
                var z = offset.Z + radius * sin;
                var normal = new Vector3(cos, 0, sin);

                vertices.Add(new Vertex(new Vector3(x, yBottom, z), normal));
                vertices.Add(new Vertex(new Vector3(x, yTop, z), normal));

                vertices.Add(new Vertex(new Vector3(x, yBottom, z), -Vector3.UnitY));
                vertices.Add(new Vertex(new Vector3(x, yTop, z), Vector3.UnitY));
            }

            for (var index = 0; index < segments; index++)
            {
                var baseIndex = startIndex + (uint)(index * 4);
                var nextBaseIndex = startIndex + (uint)((index + 1) * 4);

                indices.Add(baseIndex);
                indices.Add(nextBaseIndex);
                indices.Add(baseIndex + 1);

                indices.Add(baseIndex + 1);
                indices.Add(nextBaseIndex);
                indices.Add(nextBaseIndex + 1);

                indices.Add(bottomCenterIndex);
                indices.Add(nextBaseIndex + 2);
                indices.Add(baseIndex + 2);

                indices.Add(topCenterIndex);
                indices.Add(baseIndex + 3);
                indices.Add(nextBaseIndex + 3);
            }

            return new Mesh([.. vertices], [.. indices]);
        }

        public static Mesh CreateCone(float baseRadius, float height, int segments, Vector3 offset)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            var yBottom = offset.Y;
            var yTop = offset.Y + height;

            var topIndex = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(offset.X, yTop, offset.Z), Vector3.UnitY));

            var bottomCenterIndex = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(offset.X, yBottom, offset.Z), -Vector3.UnitY));

            var startIndex = (uint)vertices.Count;

            for (var index = 0; index <= segments; index++)
            {
                var theta = (float)index / segments * MathHelper.TwoPi;
                var cos = MathF.Cos(theta);
                var sin = MathF.Sin(theta);

                var x = offset.X + baseRadius * cos;
                var z = offset.Z + baseRadius * sin;
                var normal = new Vector3(cos, baseRadius / height, sin).Normalized();

                vertices.Add(new Vertex(new Vector3(x, yBottom, z), normal));
                vertices.Add(new Vertex(new Vector3(x, yBottom, z), -Vector3.UnitY));
            }

            for (var index = 0; index < segments; index++)
            {
                var baseIndex = startIndex + (uint)(index * 2);
                var nextBaseIndex = startIndex + (uint)((index + 1) * 2);

                indices.Add(baseIndex);
                indices.Add(nextBaseIndex);
                indices.Add(topIndex);

                indices.Add(bottomCenterIndex);
                indices.Add(nextBaseIndex + 1);
                indices.Add(baseIndex + 1);
            }

            return new Mesh([.. vertices], [.. indices]);
        }

        public static Mesh CreateTorus(float majorRadius, float minorRadius, int majorSegments, int minorSegments)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            for (var indexI = 0; indexI <= majorSegments; indexI++)
            {
                var u = (float)indexI / majorSegments * MathHelper.TwoPi;
                var cosU = MathF.Cos(u);
                var sinU = MathF.Sin(u);

                for (var indexJ = 0; indexJ <= minorSegments; indexJ++)
                {
                    var v = (float)indexJ / minorSegments * MathHelper.TwoPi;
                    var cosV = MathF.Cos(v);
                    var sinV = MathF.Sin(v);

                    var x = (majorRadius + minorRadius * cosV) * cosU;
                    var y = minorRadius * sinV;
                    var z = (majorRadius + minorRadius * cosV) * sinU;

                    var normalX = cosV * cosU;
                    var normalY = sinV;
                    var normalZ = cosV * sinU;

                    vertices.Add(new Vertex(new Vector3(x, y, z), new Vector3(normalX, normalY, normalZ)));
                }
            }

            for (var indexI = 0; indexI < majorSegments; indexI++)
            {
                for (var indexJ = 0; indexJ < minorSegments; indexJ++)
                {
                    var current = (uint)(indexI * (minorSegments + 1) + indexJ);
                    var next = (uint)((indexI + 1) * (minorSegments + 1) + indexJ);

                    indices.Add(current);
                    indices.Add(next);
                    indices.Add(current + 1);

                    indices.Add(current + 1);
                    indices.Add(next);
                    indices.Add(next + 1);
                }
            }

            return new Mesh([.. vertices], [.. indices]);
        }

        public static Mesh CreateSphere(float radius, int rings, int segments, Vector3 center)
        {
            rings = System.Math.Max(3, rings);
            segments = System.Math.Max(3, segments);

            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            for (var ring = 0; ring <= rings; ring++)
            {
                var v = ring / (float)rings;
                var phi = v * MathHelper.Pi;
                var sinPhi = MathF.Sin(phi);
                var cosPhi = MathF.Cos(phi);

                for (var segment = 0; segment <= segments; segment++)
                {
                    var u = segment / (float)segments;
                    var theta = u * MathHelper.TwoPi;
                    var sinTheta = MathF.Sin(theta);
                    var cosTheta = MathF.Cos(theta);

                    var normal = new Vector3(cosTheta * sinPhi, cosPhi, sinTheta * sinPhi);
                    var position = center + normal * radius;

                    vertices.Add(new Vertex(position, normal));
                }
            }

            for (var ring = 0; ring < rings; ring++)
            {
                for (var segment = 0; segment < segments; segment++)
                {
                    var current = (uint)(ring * (segments + 1) + segment);
                    var next = (uint)((ring + 1) * (segments + 1) + segment);

                    indices.Add(current);
                    indices.Add(next);
                    indices.Add(current + 1);

                    indices.Add(current + 1);
                    indices.Add(next);
                    indices.Add(next + 1);
                }
            }

            return new Mesh([.. vertices], [.. indices]);
        }

        public static Mesh Combine(Mesh meshA, Mesh meshB)
        {
            var vertices = new Vertex[meshA.Vertices.Length + meshB.Vertices.Length];
            meshA.Vertices.CopyTo(vertices, 0);
            meshB.Vertices.CopyTo(vertices, meshA.Vertices.Length);

            var indices = new uint[meshA.Indices.Length + meshB.Indices.Length];
            meshA.Indices.CopyTo(indices, 0);

            var offset = (uint)meshA.Vertices.Length;

            for (var index = 0; index < meshB.Indices.Length; index++)
            {
                indices[meshA.Indices.Length + index] = meshB.Indices[index] + offset;
            }

            return new Mesh(vertices, indices);
        }

        #endregion
    }
}
