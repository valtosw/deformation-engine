using CSparse;
using CSparse.Double.Factorization;
using CSparse.Storage;
using Deformation.Abstractions.Geometry;
using Deformation.Abstractions.Math;
using Deformation.Modifiers.Abstractions;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers.Arap
{
    internal sealed class ArapExactSolver : IArapSolver
    {
        #region Fields

        private const int ParallelRotationVertexLimit = 2000;

        private Rotation3x3[] _rotations = [];
        private int[] _unknownIndexByVertex = [];
        private int[] _vertexIndexByUnknown = [];
        private SparseCholesky? _factorization;

        private double[] _rightHandSideX = [];
        private double[] _rightHandSideY = [];
        private double[] _rightHandSideZ = [];
        private double[] _solutionX = [];
        private double[] _solutionY = [];
        private double[] _solutionZ = [];

        private int _factorizationVersion = -1;
        private bool _isUnavailable;

        #endregion

        #region Properties

        public bool IsUnavailable => _isUnavailable;

        #endregion

        #region Public Logic

        public void Clear()
        {
            _rotations = [];
            _unknownIndexByVertex = [];
            _vertexIndexByUnknown = [];
            _factorization = null;

            _rightHandSideX = [];
            _rightHandSideY = [];
            _rightHandSideZ = [];
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

        public void Initialize(int vertexCount)
        {
            _rotations = new Rotation3x3[vertexCount];
            Array.Fill(_rotations, Rotation3x3.Identity);

            Invalidate();
        }

        public bool TryPrepare(ArapSolverContext context)
        {
            if (_isUnavailable)
            {
                return false;
            }

            try
            {
                EnsureFactorization(context.Constrained, context.Neighbors, context.ConstraintVersion);
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

        public void Solve(ArapSolverContext context)
        {
            var isPrepared = TryPrepare(context);

            if (!isPrepared)
            {
                return;
            }

            PrepareConstraints(context);

            if (context.UseIdentityRotations)
            {
                Array.Fill(_rotations, Rotation3x3.Identity);
                SolveGlobalStep(context);

                return;
            }

            for (var iteration = 0; iteration < context.Iterations; iteration++)
            {
                EstimateLocalRotations(context.OriginalPositions, context.WorkingPositions, context.Neighbors);
                SolveGlobalStep(context);
            }
        }

        public void ApplyDeformation(Mesh mesh, ArapSolverContext context)
        {
            for (var index = 0; index < mesh.Vertices.Length; index++)
            {
                var position = context.WorkingPositions[index];
                var normal = mesh.Vertices[index].Normal;
                var textureCoordinates = mesh.Vertices[index].TexCoords;

                mesh.Vertices[index] = new Vertex(position, normal, textureCoordinates);
            }
        }

        #endregion

        #region Private Logic

        private static void PrepareConstraints(ArapSolverContext context)
        {
            context.OriginalPositions.CopyTo(context.WorkingPositions, 0);

            foreach (var controlVertex in context.ControlVertices)
            {
                var newPosition = context.TransformControlPoint(context.OriginalPositions[controlVertex]);

                context.ConstraintPositions[controlVertex] = newPosition;
                context.WorkingPositions[controlVertex] = newPosition;
            }

            for (var index = 0; index < context.Constrained.Length; index++)
            {
                var isAnchorVertex = context.Constrained[index] && !context.ControlVertices.Contains(index);

                if (isAnchorVertex)
                {
                    var originalPosition = context.OriginalPositions[index];

                    context.ConstraintPositions[index] = originalPosition;
                    context.WorkingPositions[index] = originalPosition;
                }
            }
        }

        private void EnsureFactorization(bool[] constrained, int[][] neighbors, int constraintVersion)
        {
            var isUpToDate = _factorizationVersion == constraintVersion && (_factorization is not null || _vertexIndexByUnknown.Length == 0);

            if (isUpToDate)
            {
                return;
            }

            var vertexCount = constrained.Length;
            var unknownIndexByVertex = Enumerable.Repeat(-1, vertexCount).ToArray();
            var vertexIndexByUnknown = new List<int>(vertexCount);

            for (var index = 0; index < vertexCount; index++)
            {
                var isConstrainedOrDisconnected = constrained[index] || neighbors[index].Length == 0;

                if (isConstrainedOrDisconnected)
                {
                    continue;
                }

                unknownIndexByVertex[index] = vertexIndexByUnknown.Count;
                vertexIndexByUnknown.Add(index);
            }

            var unknownCount = vertexIndexByUnknown.Count;

            _unknownIndexByVertex = unknownIndexByVertex;
            _vertexIndexByUnknown = vertexIndexByUnknown.ToArray();

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

                foreach (var neighborUnknownIndex in neighbors[index]
                             .Select(neighbor => unknownIndexByVertex[neighbor])
                             .Where(neighborUnknownIndex => neighborUnknownIndex >= 0))
                {
                    entries.Add((unknownIndex, neighborUnknownIndex, -1d));
                }
            }

            var coefficientMatrix = CompressedColumnStorage<double>.OfIndexed(unknownCount, unknownCount, entries);

            _factorization = SparseCholesky.Create(coefficientMatrix, ColumnOrdering.MinimumDegreeAtPlusA);
            _factorizationVersion = constraintVersion;
        }

        private void EnsureBuffers(int unknownCount)
        {
            if (_rightHandSideX.Length == unknownCount)
            {
                return;
            }

            _rightHandSideX = new double[unknownCount];
            _rightHandSideY = new double[unknownCount];
            _rightHandSideZ = new double[unknownCount];
            _solutionX = new double[unknownCount];
            _solutionY = new double[unknownCount];
            _solutionZ = new double[unknownCount];
        }

        private void EstimateLocalRotations(Vector3[] originalPositions, Vector3[] workingPositions, int[][] neighbors)
        {
            if (originalPositions.Length >= ParallelRotationVertexLimit)
            {
                Parallel.For(0, originalPositions.Length, index =>
                {
                    EstimateLocalRotation(index, originalPositions, workingPositions, neighbors);
                });

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

            var c11 = 0d;
            var c12 = 0d;
            var c13 = 0d;
            var c21 = 0d;
            var c22 = 0d;
            var c23 = 0d;
            var c31 = 0d;
            var c32 = 0d;
            var c33 = 0d;

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

        private void SolveGlobalStep(ArapSolverContext context)
        {
            var unknownCount = _vertexIndexByUnknown.Length;

            if (unknownCount == 0)
            {
                return;
            }

            Array.Clear(_rightHandSideX, 0, unknownCount);
            Array.Clear(_rightHandSideY, 0, unknownCount);
            Array.Clear(_rightHandSideZ, 0, unknownCount);

            for (var unknownIndex = 0; unknownIndex < unknownCount; unknownIndex++)
            {
                var index = _vertexIndexByUnknown[unknownIndex];
                var rotation = _rotations[index];

                foreach (var neighbor in context.Neighbors[index])
                {
                    var restEdge = context.OriginalPositions[index] - context.OriginalPositions[neighbor];
                    var rotatedEdge = 0.5d * (rotation.Transform(restEdge) + _rotations[neighbor].Transform(restEdge));

                    _rightHandSideX[unknownIndex] += rotatedEdge.X;
                    _rightHandSideY[unknownIndex] += rotatedEdge.Y;
                    _rightHandSideZ[unknownIndex] += rotatedEdge.Z;

                    if (context.Constrained[neighbor])
                    {
                        var constraintPosition = context.ConstraintPositions[neighbor];

                        _rightHandSideX[unknownIndex] += constraintPosition.X;
                        _rightHandSideY[unknownIndex] += constraintPosition.Y;
                        _rightHandSideZ[unknownIndex] += constraintPosition.Z;
                    }
                }
            }

            if (_factorization is null)
            {
                return;
            }

            _factorization.Solve(_rightHandSideX, _solutionX);
            _factorization.Solve(_rightHandSideY, _solutionY);
            _factorization.Solve(_rightHandSideZ, _solutionZ);

            for (var unknownIndex = 0; unknownIndex < unknownCount; unknownIndex++)
            {
                var newPosition = new Vector3(
                    (float)_solutionX[unknownIndex],
                    (float)_solutionY[unknownIndex],
                    (float)_solutionZ[unknownIndex]);

                context.WorkingPositions[_vertexIndexByUnknown[unknownIndex]] = newPosition;
            }
        }

        private void MarkUnavailable()
        {
            _isUnavailable = true;
            _factorization = null;
        }

        #endregion
    }
}