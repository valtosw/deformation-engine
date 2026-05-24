using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers
{
    public sealed class FfdDeformer : IDeformer
    {
        #region Structs

        private readonly struct FfdWeight
        {
            public int ControlPointIndex { get; init; }
            public float Weight { get; init; }
        }

        #endregion

        #region Fields

        private Vector3[]? _flatControlPoints;
        private FfdWeight[][]? _precomputedWeights;

        private int _resolutionX;
        private int _resolutionY;
        private int _resolutionZ;

        #endregion

        #region Public Logic

        public void Initialize(Mesh originalMesh, int resolutionX, int resolutionY, int resolutionZ)
        {
            _resolutionX = Math.Max(2, resolutionX);
            _resolutionY = Math.Max(2, resolutionY);
            _resolutionZ = Math.Max(2, resolutionZ);

            var boundingBox = originalMesh.LocalBoundingBox;
            var size = boundingBox.Size;

            _flatControlPoints = new Vector3[_resolutionX * _resolutionY * _resolutionZ];

            for (var indexX = 0; indexX < _resolutionX; indexX++)
            {
                for (var indexY = 0; indexY < _resolutionY; indexY++)
                {
                    for (var indexZ = 0; indexZ < _resolutionZ; indexZ++)
                    {
                        var u = indexX / (float)(_resolutionX - 1);
                        var v = indexY / (float)(_resolutionY - 1);
                        var w = indexZ / (float)(_resolutionZ - 1);

                        var flatIndex = indexX * (_resolutionY * _resolutionZ) + indexY * _resolutionZ + indexZ;

                        _flatControlPoints[flatIndex] = new Vector3(
                            boundingBox.Min.X + u * size.X,
                            boundingBox.Min.Y + v * size.Y,
                            boundingBox.Min.Z + w * size.Z
                        );
                    }
                }
            }

            _precomputedWeights = new FfdWeight[originalMesh.Vertices.Length][];

            Parallel.For(0, originalMesh.Vertices.Length, index =>
            {
                var position = originalMesh.Vertices[index].Position;

                var parametricX = size.X > MathConstants.LengthTolerance ? (position.X - boundingBox.Min.X) / size.X : 0f;
                var parametricY = size.Y > MathConstants.LengthTolerance ? (position.Y - boundingBox.Min.Y) / size.Y : 0f;
                var parametricZ = size.Z > MathConstants.LengthTolerance ? (position.Z - boundingBox.Min.Z) / size.Z : 0f;

                var weights = new List<FfdWeight>();

                for (var indexX = 0; indexX < _resolutionX; indexX++)
                {
                    var weightX = CalculateBernsteinPolynomial(_resolutionX - 1, indexX, parametricX);

                    for (var indexY = 0; indexY < _resolutionY; indexY++)
                    {
                        var weightY = CalculateBernsteinPolynomial(_resolutionY - 1, indexY, parametricY);

                        for (var indexZ = 0; indexZ < _resolutionZ; indexZ++)
                        {
                            var weightZ = CalculateBernsteinPolynomial(_resolutionZ - 1, indexZ, parametricZ);
                            var totalWeight = weightX * weightY * weightZ;

                            if (totalWeight > MathConstants.ZeroTolerance)
                            {
                                var flatIndex = indexX * (_resolutionY * _resolutionZ) + indexY * _resolutionZ + indexZ;
                                weights.Add(new FfdWeight { ControlPointIndex = flatIndex, Weight = totalWeight });
                            }
                        }
                    }
                }

                _precomputedWeights[index] = [.. weights];
            });
        }

        public void UpdateControlPoint(int indexX, int indexY, int indexZ, Vector3 position)
        {
            if (_flatControlPoints is not null)
            {
                var flatIndex = indexX * (_resolutionY * _resolutionZ) + indexY * _resolutionZ + indexZ;
                _flatControlPoints[flatIndex] = position;
            }
        }

        public Vector3 GetControlPoint(int indexX, int indexY, int indexZ)
        {
            if (_flatControlPoints is not null)
            {
                var flatIndex = indexX * (_resolutionY * _resolutionZ) + indexY * _resolutionZ + indexZ;
                return _flatControlPoints[flatIndex];
            }

            return Vector3.Zero;
        }

        public void Deform(Mesh mesh)
        {
            if (_flatControlPoints is null || _precomputedWeights is null || _precomputedWeights.Length != mesh.Vertices.Length)
            {
                return;
            }

            Parallel.For(0, mesh.Vertices.Length, index =>
            {
                var newPosition = Vector3.Zero;
                var weights = _precomputedWeights[index];

                for (var weightIndex = 0; weightIndex < weights.Length; weightIndex++)
                {
                    var weightData = weights[weightIndex];
                    newPosition += _flatControlPoints[weightData.ControlPointIndex] * weightData.Weight;
                }

                mesh.Vertices[index].Position = newPosition;
            });

            RecalculateNormals(mesh);
        }

        #endregion

        #region Private Logic

        private static void RecalculateNormals(Mesh mesh)
        {
            if (mesh.Topology != MeshTopology.Triangles)
            {
                return;
            }

            var normals = new Vector3[mesh.Vertices.Length];

            for (var index = 0; index < mesh.Indices.Length; index += 3)
            {
                var index0 = mesh.Indices[index];
                var index1 = mesh.Indices[index + 1];
                var index2 = mesh.Indices[index + 2];

                var vertex0 = mesh.Vertices[index0].Position;
                var vertex1 = mesh.Vertices[index1].Position;
                var vertex2 = mesh.Vertices[index2].Position;

                var normal = Vector3.Cross(vertex1 - vertex0, vertex2 - vertex0);

                normals[index0] += normal;
                normals[index1] += normal;
                normals[index2] += normal;
            }

            Parallel.For(0, mesh.Vertices.Length, index =>
            {
                if (normals[index].LengthSquared > MathConstants.LengthTolerance)
                {
                    mesh.Vertices[index].Normal = normals[index].Normalized();
                }
            });
        }

        private static float CalculateBernsteinPolynomial(int degree, int index, float parameter)
        {
            var clampedParameter = Math.Clamp(parameter, 0f, 1f);
            var binomialCoefficient = CalculateBinomialCoefficient(degree, index);

            return binomialCoefficient * MathF.Pow(clampedParameter, index) * MathF.Pow(1f - clampedParameter, degree - index);
        }

        private static int CalculateBinomialCoefficient(int n, int k)
        {
            if (k < 0 || k > n)
            {
                return 0;
            }

            if (k == 0 || k == n)
            {
                return 1;
            }

            k = Math.Min(k, n - k);
            var coefficient = 1;

            for (var index = 0; index < k; index++)
            {
                coefficient = coefficient * (n - index) / (index + 1);
            }

            return coefficient;
        }

        #endregion
    }
}