using CSparse;
using CSparse.Double.Factorization;
using CSparse.Storage;
using Deformation.Abstractions.Math;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers.Arap
{
    internal sealed class ArapExactSolver
    {
        private const int ParallelRotationVertexLimit = 2000;

        private Rotation3x3[] _rotations = [];
        private int[] _unknownIndexByVertex = [];
        private int[] _vertexIndexByUnknown = [];
        private SparseCholesky? _factorization;
        private double[] _rhsX = [];
        private double[] _rhsY = [];
        private double[] _rhsZ = [];
        private double[] _solutionX = [];
        private double[] _solutionY = [];
        private double[] _solutionZ = [];
        private int _factorizationVersion = -1;
        private bool _isUnavailable;

        public bool IsUnavailable => _isUnavailable;

        public void Reset(int vertexCount)
        {
            _rotations = new Rotation3x3[vertexCount];
            Array.Fill(_rotations, Rotation3x3.Identity);
            Invalidate();
            _isUnavailable = false;
        }

        public void Clear()
        {
            _rotations = [];
            _unknownIndexByVertex = [];
            _vertexIndexByUnknown = [];
            _factorization = null;
            _rhsX = [];
            _rhsY = [];
            _rhsZ = [];
            _solutionX = [];
            _solutionY = [];
            _solutionZ = [];
            _factorizationVersion = -1;
            _isUnavailable = false;
        }

        public void Invalidate()
        {
            _unknownIndexByVertex = [];
            _vertexIndexByUnknown = [];
            _factorization = null;
            _factorizationVersion = -1;
            _isUnavailable = false;
        }

        public bool TryPrepare(bool[] constrained, int[][] neighbors, int constraintVersion)
        {
            if (_isUnavailable)
            {
                return false;
            }

            try
            {
                EnsureFactorization(constrained, neighbors, constraintVersion);
                return true;
            }
            catch (InvalidOperationException)
            {
                MarkUnavailable();
                return false;
            }
            catch (ArgumentException)
            {
                MarkUnavailable();
                return false;
            }
        }

        public void Solve(
            Vector3[] originalPositions,
            Vector3[] workingPositions,
            Vector3[] constraintPositions,
            int[][] neighbors,
            bool[] constrained,
            int constraintVersion,
            int iterations,
            bool useIdentityRotations)
        {
            if (!TryPrepare(constrained, neighbors, constraintVersion))
            {
                return;
            }

            if (useIdentityRotations)
            {
                Array.Fill(_rotations, Rotation3x3.Identity);
                SolveGlobalStep(originalPositions, workingPositions, constraintPositions, neighbors, constrained);
                return;
            }

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                EstimateLocalRotations(originalPositions, workingPositions, neighbors);
                SolveGlobalStep(originalPositions, workingPositions, constraintPositions, neighbors, constrained);
            }
        }

        private void EnsureFactorization(bool[] constrained, int[][] neighbors, int constraintVersion)
        {
            if (_factorizationVersion == constraintVersion &&
                (_factorization is not null || _vertexIndexByUnknown.Length == 0))
            {
                return;
            }

            var vertexCount = constrained.Length;
            var unknownIndexByVertex = Enumerable.Repeat(-1, vertexCount).ToArray();
            var vertexIndexByUnknown = new List<int>(vertexCount);

            for (var index = 0; index < vertexCount; index++)
            {
                if (constrained[index] || neighbors[index].Length == 0)
                {
                    continue;
                }

                unknownIndexByVertex[index] = vertexIndexByUnknown.Count;
                vertexIndexByUnknown.Add(index);
            }

            var unknownCount = vertexIndexByUnknown.Count;

            _unknownIndexByVertex = unknownIndexByVertex;
            _vertexIndexByUnknown = [.. vertexIndexByUnknown];
            EnsureBuffers(unknownCount);

            if (unknownCount == 0)
            {
                _factorization = null;
                _factorizationVersion = constraintVersion;
                return;
            }

            var entries = new List<(int Row, int Column, double Value)>(unknownCount * 7);

            for (var unknownIndex = 0; unknownIndex < unknownCount; unknownIndex++)
            {
                var index = _vertexIndexByUnknown[unknownIndex];
                entries.Add((unknownIndex, unknownIndex, neighbors[index].Length));

                foreach (var neighbor in neighbors[index])
                {
                    var neighborUnknownIndex = unknownIndexByVertex[neighbor];

                    if (neighborUnknownIndex >= 0)
                    {
                        entries.Add((unknownIndex, neighborUnknownIndex, -1d));
                    }
                }
            }

            var coefficientMatrix = CompressedColumnStorage<double>.OfIndexed(unknownCount, unknownCount, entries);
            _factorization = SparseCholesky.Create(coefficientMatrix, ColumnOrdering.MinimumDegreeAtPlusA);
            _factorizationVersion = constraintVersion;
        }

        private void EnsureBuffers(int unknownCount)
        {
            if (_rhsX.Length == unknownCount)
            {
                return;
            }

            _rhsX = new double[unknownCount];
            _rhsY = new double[unknownCount];
            _rhsZ = new double[unknownCount];
            _solutionX = new double[unknownCount];
            _solutionY = new double[unknownCount];
            _solutionZ = new double[unknownCount];
        }

        private void EstimateLocalRotations(Vector3[] originalPositions, Vector3[] workingPositions, int[][] neighbors)
        {
            if (originalPositions.Length >= ParallelRotationVertexLimit)
            {
                Parallel.For(0, originalPositions.Length, index => EstimateLocalRotation(index, originalPositions, workingPositions, neighbors));
                return;
            }

            for (var index = 0; index < originalPositions.Length; index++)
            {
                EstimateLocalRotation(index, originalPositions, workingPositions, neighbors);
            }
        }

        private void EstimateLocalRotation(int index, Vector3[] originalPositions, Vector3[] workingPositions, int[][] neighbors)
        {
            if (neighbors[index].Length == 0)
            {
                _rotations[index] = Rotation3x3.Identity;
                return;
            }

            double c11 = 0d;
            double c12 = 0d;
            double c13 = 0d;
            double c21 = 0d;
            double c22 = 0d;
            double c23 = 0d;
            double c31 = 0d;
            double c32 = 0d;
            double c33 = 0d;

            foreach (var neighbor in neighbors[index])
            {
                var restEdge = originalPositions[index] - originalPositions[neighbor];
                var deformedEdge = workingPositions[index] - workingPositions[neighbor];

                c11 += deformedEdge.X * restEdge.X;
                c12 += deformedEdge.X * restEdge.Y;
                c13 += deformedEdge.X * restEdge.Z;
                c21 += deformedEdge.Y * restEdge.X;
                c22 += deformedEdge.Y * restEdge.Y;
                c23 += deformedEdge.Y * restEdge.Z;
                c31 += deformedEdge.Z * restEdge.X;
                c32 += deformedEdge.Z * restEdge.Y;
                c33 += deformedEdge.Z * restEdge.Z;
            }

            _rotations[index] = Rotation3x3.FromCovariance(c11, c12, c13, c21, c22, c23, c31, c32, c33);
        }

        private void SolveGlobalStep(
            Vector3[] originalPositions,
            Vector3[] workingPositions,
            Vector3[] constraintPositions,
            int[][] neighbors,
            bool[] constrained)
        {
            var unknownCount = _vertexIndexByUnknown.Length;

            if (unknownCount == 0)
            {
                return;
            }

            Array.Clear(_rhsX, 0, unknownCount);
            Array.Clear(_rhsY, 0, unknownCount);
            Array.Clear(_rhsZ, 0, unknownCount);

            for (var unknownIndex = 0; unknownIndex < unknownCount; unknownIndex++)
            {
                var index = _vertexIndexByUnknown[unknownIndex];
                var rotation = _rotations[index];

                foreach (var neighbor in neighbors[index])
                {
                    var restEdge = originalPositions[index] - originalPositions[neighbor];
                    var rotatedEdge = 0.5d * (rotation.Transform(restEdge) + _rotations[neighbor].Transform(restEdge));

                    _rhsX[unknownIndex] += rotatedEdge.X;
                    _rhsY[unknownIndex] += rotatedEdge.Y;
                    _rhsZ[unknownIndex] += rotatedEdge.Z;

                    if (constrained[neighbor])
                    {
                        var constraintPosition = constraintPositions[neighbor];
                        _rhsX[unknownIndex] += constraintPosition.X;
                        _rhsY[unknownIndex] += constraintPosition.Y;
                        _rhsZ[unknownIndex] += constraintPosition.Z;
                    }
                }
            }

            if (_factorization is null)
            {
                return;
            }

            _factorization.Solve(_rhsX, _solutionX);
            _factorization.Solve(_rhsY, _solutionY);
            _factorization.Solve(_rhsZ, _solutionZ);

            for (var unknownIndex = 0; unknownIndex < unknownCount; unknownIndex++)
            {
                workingPositions[_vertexIndexByUnknown[unknownIndex]] = new Vector3(
                    (float)_solutionX[unknownIndex],
                    (float)_solutionY[unknownIndex],
                    (float)_solutionZ[unknownIndex]);
            }
        }

        private void MarkUnavailable()
        {
            _isUnavailable = true;
            _factorization = null;
        }
    }
}
