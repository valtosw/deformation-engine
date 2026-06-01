using Deformation.Abstractions.Comparers;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using CSparse;
using CSparse.Double.Factorization;
using CSparse.Storage;
using OpenTK.Mathematics;
using System.Threading.Tasks;

namespace Deformation.Modifiers.Deformers
{
    public sealed class ArapDeformer : IDeformer
    {
        #region Constants

        private const int ExactSolveVertexLimit = 75000;
        private const int ExactSolveUnknownLimit = 60000;
        private const int ParallelRotationVertexLimit = 2000;
        private const int PolarRotationIterations = 8;
        private const int PreviewGraphDepth = 48;

        #endregion

        #region Fields

        private readonly HashSet<int> _controlVertices = [];
        private readonly HashSet<int> _manualAnchorVertices = [];
        private readonly HashSet<int> _distanceAnchorVertices = [];

        private Vector3[] _originalPositions = [];
        private Vector3[] _workingPositions = [];
        private Vector3[] _constraintPositions = [];
        private Rotation3x3[] _rotations = [];
        private int[][] _neighbors = [];
        private int[][] _weldedVertexGroups = [];
        private bool[] _constraintMask = [];
        private bool _constraintMaskDirty = true;
        private int[] _unknownIndexByVertex = [];
        private int[] _vertexIndexByUnknown = [];
        private SparseCholesky? _coefficientFactorization;
        private double[] _rhsX = [];
        private double[] _rhsY = [];
        private double[] _rhsZ = [];
        private double[] _solutionX = [];
        private double[] _solutionY = [];
        private double[] _solutionZ = [];
        private float[] _previewWeights = [];
        private bool _previewWeightsDirty = true;
        private bool _exactSolveUnavailable;
        private int _constraintSystemVersion;
        private int _factorizationVersion = -1;

        private Vector3 _pivot = Vector3.Zero;
        private Vector3 _handlePosition = Vector3.Zero;
        private Quaternion _handleRotation = Quaternion.Identity;

        private bool _isInitialized;
        private bool _distanceAnchorsDirty = true;

        #endregion

        #region Events

        public event EventHandler? SelectionChanged;
        public event EventHandler? DeformationChanged;

        #endregion

        #region Properties

        public ArapAnchorType AnchorType { get; private set; } = ArapAnchorType.Manual;
        public ArapActionMode ActionMode { get; private set; } = ArapActionMode.ControlPoints;

        public float AnchorDistance { get; private set; } = 0.25f;
        public int Iterations { get; private set; } = 12;

        public IReadOnlyCollection<int> ControlVertices => _controlVertices;
        public IReadOnlyCollection<int> AnchorVertices
        {
            get
            {
                EnsureDistanceAnchors();
                return AnchorType == ArapAnchorType.Distance ? _distanceAnchorVertices : _manualAnchorVertices;
            }
        }

        public Vector3 Pivot => _pivot;
        public bool IsInitialized => _isInitialized;
        public bool HasSelection => _controlVertices.Count > 0 || AnchorVertices.Count > 0;
        public bool HasChanges => _controlVertices.Count > 0 && (_handlePosition != _pivot || _handleRotation != Quaternion.Identity);
        public Vector3 HandlePosition => _handlePosition;
        public Quaternion HandleRotation => _handleRotation;
        public int VertexCount => _originalPositions.Length;

        #endregion

        #region Public Logic

        public void Initialize(Mesh originalMesh)
        {
            _originalPositions = originalMesh.Vertices.Select(vertex => vertex.Position).ToArray();
            _workingPositions = new Vector3[_originalPositions.Length];
            _constraintPositions = new Vector3[_originalPositions.Length];
            _rotations = new Rotation3x3[_originalPositions.Length];
            _previewWeights = new float[_originalPositions.Length];

            FillIdentityRotations();

            BuildTopology(originalMesh);

            _controlVertices.Clear();
            _manualAnchorVertices.Clear();
            _distanceAnchorVertices.Clear();
            _pivot = originalMesh.LocalBoundingBox.Center;
            _handlePosition = _pivot;
            _handleRotation = Quaternion.Identity;
            _distanceAnchorsDirty = true;
            InvalidateConstrainedSystem();
            _isInitialized = true;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            _controlVertices.Clear();
            _manualAnchorVertices.Clear();
            _distanceAnchorVertices.Clear();
            _originalPositions = [];
            _workingPositions = [];
            _constraintPositions = [];
            _rotations = [];
            _neighbors = [];
            _weldedVertexGroups = [];
            _constraintMask = [];
            _constraintMaskDirty = true;
            _unknownIndexByVertex = [];
            _vertexIndexByUnknown = [];
            _coefficientFactorization = null;
            _rhsX = [];
            _rhsY = [];
            _rhsZ = [];
            _solutionX = [];
            _solutionY = [];
            _solutionZ = [];
            _previewWeights = [];
            _previewWeightsDirty = true;
            _exactSolveUnavailable = false;
            _constraintSystemVersion = 0;
            _factorizationVersion = -1;
            _pivot = Vector3.Zero;
            _handlePosition = Vector3.Zero;
            _handleRotation = Quaternion.Identity;
            _isInitialized = false;
            _distanceAnchorsDirty = true;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Reset()
        {
            _handlePosition = _pivot;
            _handleRotation = Quaternion.Identity;
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetAnchorType(ArapAnchorType anchorType)
        {
            if (AnchorType == anchorType)
            {
                return;
            }

            AnchorType = anchorType;
            _distanceAnchorsDirty = true;
            InvalidateConstrainedSystem();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetActionMode(ArapActionMode actionMode)
        {
            if (ActionMode == actionMode)
            {
                return;
            }

            ActionMode = actionMode;
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetAnchorDistance(float distance)
        {
            var clampedDistance = Math.Clamp(distance, 0f, 1f);

            if (Math.Abs(AnchorDistance - clampedDistance) < MathConstants.ZeroTolerance)
            {
                return;
            }

            AnchorDistance = clampedDistance;
            _distanceAnchorsDirty = true;
            InvalidateConstrainedSystem();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetIterations(int iterations)
        {
            var clampedIterations = Math.Clamp(iterations, 1, 50);

            if (Iterations == clampedIterations)
            {
                return;
            }

            Iterations = clampedIterations;
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void PaintVertices(IEnumerable<int> vertexIndices, bool erase)
        {
            if (!_isInitialized)
            {
                return;
            }

            var changed = false;

            foreach (var vertexIndex in ExpandWeldedVertices(vertexIndices))
            {
                if (ActionMode == ArapActionMode.ControlPoints)
                {
                    changed |= erase ? _controlVertices.Remove(vertexIndex) : _controlVertices.Add(vertexIndex);

                    if (!erase)
                    {
                        _manualAnchorVertices.Remove(vertexIndex);
                    }
                }
                else if (ActionMode == ArapActionMode.AnchorPoints && AnchorType == ArapAnchorType.Manual)
                {
                    changed |= erase ? _manualAnchorVertices.Remove(vertexIndex) : _manualAnchorVertices.Add(vertexIndex);

                    if (!erase)
                    {
                        _controlVertices.Remove(vertexIndex);
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            RecalculatePivot();
            _distanceAnchorsDirty = true;
            InvalidateConstrainedSystem();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyList<int> GetVerticesWithinBrush(Vector3 center, float radius)
        {
            if (!_isInitialized)
            {
                return [];
            }

            var radiusSquared = radius * radius;
            var vertices = new List<int>();

            for (var index = 0; index < _originalPositions.Length; index++)
            {
                if ((_originalPositions[index] - center).LengthSquared <= radiusSquared)
                {
                    vertices.Add(index);
                }
            }

            return vertices;
        }

        public Vector3 GetOriginalPosition(int vertexIndex)
        {
            return vertexIndex >= 0 && vertexIndex < _originalPositions.Length
                ? _originalPositions[vertexIndex]
                : Vector3.Zero;
        }

        public Vector3 GetCurrentPosition(int vertexIndex)
        {
            if (vertexIndex < 0 || vertexIndex >= _originalPositions.Length)
            {
                return Vector3.Zero;
            }

            if (ActionMode == ArapActionMode.Deform && _controlVertices.Contains(vertexIndex))
            {
                return TransformControlPoint(_originalPositions[vertexIndex]);
            }

            return HasChanges && vertexIndex < _workingPositions.Length
                ? _workingPositions[vertexIndex]
                : _originalPositions[vertexIndex];
        }

        public void BeginDeform()
        {
            RecalculatePivot();
            _handlePosition = _pivot;
            _handleRotation = Quaternion.Identity;
            EnsureDistanceAnchors();

            var constrained = GetConstraintMask();

            if (ShouldUsePreviewSolve(constrained))
            {
                EnsurePreviewWeights(constrained);
            }
            else
            {
                TryEnsureCoefficientFactorization(constrained);
            }

            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetHandleTransform(Vector3 position, Quaternion rotation)
        {
            if (_handlePosition == position && _handleRotation == rotation)
            {
                return;
            }

            _handlePosition = position;
            _handleRotation = rotation;
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Deform(Mesh mesh)
        {
            if (!_isInitialized || _originalPositions.Length != mesh.Vertices.Length || _controlVertices.Count == 0)
            {
                return;
            }

            if (ActionMode != ArapActionMode.Deform || !HasChanges)
            {
                return;
            }

            EnsureDistanceAnchors();
            var constrained = GetConstraintMask();
            var usePreviewSolve = ShouldUsePreviewSolve(constrained) || !TryEnsureCoefficientFactorization(constrained);

            if (usePreviewSolve)
            {
                SolvePreviewShape(constrained);
                ApplyPreviewPositions(mesh);
                return;
            }
            else
            {
                SolveConstrainedShape(constrained);
            }

            for (var index = 0; index < mesh.Vertices.Length; index++)
            {
                mesh.Vertices[index] = new Vertex(_workingPositions[index], mesh.Vertices[index].Normal, mesh.Vertices[index].TexCoords);
            }

            RecalculateNormals(mesh);
        }

        #endregion

        #region Private Logic

        private void BuildTopology(Mesh mesh)
        {
            var neighborSets = Enumerable.Range(0, mesh.Vertices.Length)
                .Select(_ => new HashSet<int>())
                .ToArray();

            if (mesh.Topology == MeshTopology.Triangles)
            {
                for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
                {
                    AddEdge((int)mesh.Indices[index], (int)mesh.Indices[index + 1]);
                    AddEdge((int)mesh.Indices[index + 1], (int)mesh.Indices[index + 2]);
                    AddEdge((int)mesh.Indices[index + 2], (int)mesh.Indices[index]);
                }
            }
            else
            {
                for (var index = 0; index + 1 < mesh.Indices.Length; index += 2)
                {
                    AddEdge((int)mesh.Indices[index], (int)mesh.Indices[index + 1]);
                }
            }

            var positionGroups = mesh.Vertices
                .Select((vertex, index) => (vertex.Position, Index: index))
                .GroupBy(item => item.Position, new Vector3EqualityComparer())
                .Select(group => group.Select(item => item.Index).ToArray())
                .ToArray();

            _weldedVertexGroups = new int[mesh.Vertices.Length][];

            foreach (var group in positionGroups)
            {
                foreach (var index in group)
                {
                    _weldedVertexGroups[index] = group;
                }

                if (group.Length <= 1)
                {
                    continue;
                }

                for (var first = 0; first < group.Length; first++)
                {
                    for (var second = first + 1; second < group.Length; second++)
                    {
                        AddEdge(group[first], group[second]);
                    }
                }
            }

            _neighbors = neighborSets.Select(set => set.ToArray()).ToArray();
            void AddEdge(int indexA, int indexB)
            {
                if (indexA == indexB ||
                    indexA < 0 ||
                    indexB < 0 ||
                    indexA >= mesh.Vertices.Length ||
                    indexB >= mesh.Vertices.Length)
                {
                    return;
                }

                neighborSets[indexA].Add(indexB);
                neighborSets[indexB].Add(indexA);
            }
        }

        private IEnumerable<int> ExpandWeldedVertices(IEnumerable<int> vertexIndices)
        {
            foreach (var vertexIndex in vertexIndices)
            {
                if (vertexIndex < 0 || vertexIndex >= _weldedVertexGroups.Length)
                {
                    continue;
                }

                foreach (var weldedIndex in _weldedVertexGroups[vertexIndex])
                {
                    yield return weldedIndex;
                }
            }
        }

        private void RecalculatePivot()
        {
            if (_controlVertices.Count == 0)
            {
                return;
            }

            var pivot = Vector3.Zero;

            foreach (var vertexIndex in _controlVertices)
            {
                pivot += _originalPositions[vertexIndex];
            }

            _pivot = pivot / _controlVertices.Count;
            _handlePosition = _pivot;
            _handleRotation = Quaternion.Identity;
        }

        private void EnsureDistanceAnchors()
        {
            if (AnchorType != ArapAnchorType.Distance || !_distanceAnchorsDirty)
            {
                return;
            }

            _distanceAnchorVertices.Clear();

            if (_controlVertices.Count == 0 || _originalPositions.Length == 0)
            {
                _distanceAnchorsDirty = false;
                return;
            }

            var bounds = AxisAlignedBoundingBox.FromPoints(_originalPositions);
            var radius = bounds.Size.Length * AnchorDistance;
            var radiusSquared = radius * radius;

            if (radius <= MathConstants.ZeroTolerance)
            {
                _distanceAnchorsDirty = false;
                return;
            }

            if (radiusSquared >= bounds.Size.LengthSquared)
            {
                for (var index = 0; index < _originalPositions.Length; index++)
                {
                    if (!_controlVertices.Contains(index))
                    {
                        _distanceAnchorVertices.Add(index);
                    }
                }

                _distanceAnchorsDirty = false;
                return;
            }

            var controlVerticesByCell = BuildControlVertexCellMap(bounds.Min, radius);

            for (var index = 0; index < _originalPositions.Length; index++)
            {
                if (_controlVertices.Contains(index))
                {
                    continue;
                }

                var cell = GetCell(_originalPositions[index], bounds.Min, radius);
                var foundAnchor = false;

                for (var offsetX = -1; offsetX <= 1 && !foundAnchor; offsetX++)
                {
                    for (var offsetY = -1; offsetY <= 1 && !foundAnchor; offsetY++)
                    {
                        for (var offsetZ = -1; offsetZ <= 1; offsetZ++)
                        {
                            var nearbyCell = (cell.X + offsetX, cell.Y + offsetY, cell.Z + offsetZ);

                            if (!controlVerticesByCell.TryGetValue(nearbyCell, out var nearbyControls))
                            {
                                continue;
                            }

                            foreach (var controlVertex in nearbyControls)
                            {
                                if ((_originalPositions[index] - _originalPositions[controlVertex]).LengthSquared > radiusSquared)
                                {
                                    continue;
                                }

                                _distanceAnchorVertices.Add(index);
                                foundAnchor = true;
                                break;
                            }

                            if (foundAnchor)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            _distanceAnchorsDirty = false;
        }

        private Dictionary<(int X, int Y, int Z), List<int>> BuildControlVertexCellMap(Vector3 origin, float cellSize)
        {
            var controlVerticesByCell = new Dictionary<(int X, int Y, int Z), List<int>>();

            foreach (var controlVertex in _controlVertices)
            {
                var cell = GetCell(_originalPositions[controlVertex], origin, cellSize);

                if (!controlVerticesByCell.TryGetValue(cell, out var controlVertices))
                {
                    controlVertices = [];
                    controlVerticesByCell[cell] = controlVertices;
                }

                controlVertices.Add(controlVertex);
            }

            return controlVerticesByCell;
        }

        private static (int X, int Y, int Z) GetCell(Vector3 position, Vector3 origin, float cellSize)
        {
            var offset = position - origin;

            return (
                (int)MathF.Floor(offset.X / cellSize),
                (int)MathF.Floor(offset.Y / cellSize),
                (int)MathF.Floor(offset.Z / cellSize));
        }

        private void SolveConstrainedShape(bool[] constrained)
        {
            _originalPositions.CopyTo(_workingPositions, 0);

            foreach (var controlVertex in _controlVertices)
            {
                _constraintPositions[controlVertex] = TransformControlPoint(_originalPositions[controlVertex]);
                _workingPositions[controlVertex] = _constraintPositions[controlVertex];
            }

            foreach (var anchorVertex in AnchorVertices)
            {
                if (_controlVertices.Contains(anchorVertex))
                {
                    continue;
                }

                constrained[anchorVertex] = true;
                _constraintPositions[anchorVertex] = _originalPositions[anchorVertex];
                _workingPositions[anchorVertex] = _constraintPositions[anchorVertex];
            }

            if (IsIdentityRotation(_handleRotation))
            {
                FillIdentityRotations();
                SolveGlobalStep(constrained, _rotations);
                return;
            }

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                EstimateLocalRotations();
                SolveGlobalStep(constrained, _rotations);
            }
        }

        private void SolvePreviewShape(bool[] constrained)
        {
            EnsurePreviewWeights(constrained);

            for (var index = 0; index < _originalPositions.Length; index++)
            {
                var weight = _previewWeights[index];

                if (weight <= MathConstants.ZeroTolerance)
                {
                    _workingPositions[index] = _originalPositions[index];
                    continue;
                }

                if (constrained[index] && !_controlVertices.Contains(index))
                {
                    _workingPositions[index] = _originalPositions[index];
                    continue;
                }

                var targetPosition = TransformControlPoint(_originalPositions[index]);
                _workingPositions[index] = Vector3.Lerp(_originalPositions[index], targetPosition, weight);
            }
        }

        private void ApplyPreviewPositions(Mesh mesh)
        {
            for (var index = 0; index < mesh.Vertices.Length; index++)
            {
                if (_previewWeights[index] <= MathConstants.ZeroTolerance)
                {
                    continue;
                }

                mesh.Vertices[index] = new Vertex(_workingPositions[index], mesh.Vertices[index].Normal, mesh.Vertices[index].TexCoords);
            }
        }

        private Vector3 TransformControlPoint(Vector3 position)
        {
            var offset = position - _pivot;
            return _handlePosition + Vector3.Transform(offset, _handleRotation);
        }

        private bool[] GetConstraintMask()
        {
            if (!_constraintMaskDirty && _constraintMask.Length == _originalPositions.Length)
            {
                return _constraintMask;
            }

            if (_constraintMask.Length != _originalPositions.Length)
            {
                _constraintMask = new bool[_originalPositions.Length];
            }
            else
            {
                Array.Clear(_constraintMask);
            }

            foreach (var controlVertex in _controlVertices)
            {
                _constraintMask[controlVertex] = true;
            }

            foreach (var anchorVertex in AnchorVertices)
            {
                if (!_controlVertices.Contains(anchorVertex))
                {
                    _constraintMask[anchorVertex] = true;
                }
            }

            StabilizeUnconstrainedComponents(_constraintMask);
            _constraintMaskDirty = false;
            return _constraintMask;
        }

        private void FillIdentityRotations()
        {
            Array.Fill(_rotations, Rotation3x3.Identity);
        }

        private void EstimateLocalRotations()
        {
            if (_originalPositions.Length >= ParallelRotationVertexLimit)
            {
                Parallel.For(0, _originalPositions.Length, EstimateLocalRotation);
                return;
            }

            for (var index = 0; index < _originalPositions.Length; index++)
            {
                EstimateLocalRotation(index);
            }
        }

        private void EstimateLocalRotation(int index)
        {
            if (_neighbors[index].Length == 0)
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

            foreach (var neighbor in _neighbors[index])
            {
                var restEdge = _originalPositions[index] - _originalPositions[neighbor];
                var deformedEdge = _workingPositions[index] - _workingPositions[neighbor];

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

            _rotations[index] = ExtractRotation(c11, c12, c13, c21, c22, c23, c31, c32, c33);
        }

        private void SolveGlobalStep(bool[] constrained, IReadOnlyList<Rotation3x3> rotations)
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
                var rotation = rotations[index];

                foreach (var neighbor in _neighbors[index])
                {
                    var restEdge = _originalPositions[index] - _originalPositions[neighbor];
                    var rotatedEdge = 0.5d * (rotation.Transform(restEdge) + rotations[neighbor].Transform(restEdge));

                    _rhsX[unknownIndex] += rotatedEdge.X;
                    _rhsY[unknownIndex] += rotatedEdge.Y;
                    _rhsZ[unknownIndex] += rotatedEdge.Z;

                    if (constrained[neighbor])
                    {
                        var constraintPosition = _constraintPositions[neighbor];
                        _rhsX[unknownIndex] += constraintPosition.X;
                        _rhsY[unknownIndex] += constraintPosition.Y;
                        _rhsZ[unknownIndex] += constraintPosition.Z;
                    }
                }
            }

            if (_coefficientFactorization is null)
            {
                return;
            }

            _coefficientFactorization.Solve(_rhsX, _solutionX);
            _coefficientFactorization.Solve(_rhsY, _solutionY);
            _coefficientFactorization.Solve(_rhsZ, _solutionZ);

            for (var unknownIndex = 0; unknownIndex < unknownCount; unknownIndex++)
            {
                _workingPositions[_vertexIndexByUnknown[unknownIndex]] = new Vector3(
                    (float)_solutionX[unknownIndex],
                    (float)_solutionY[unknownIndex],
                    (float)_solutionZ[unknownIndex]);
            }
        }

        private static bool IsIdentityRotation(Quaternion rotation)
        {
            return MathF.Abs(rotation.X) < MathConstants.ZeroTolerance &&
                   MathF.Abs(rotation.Y) < MathConstants.ZeroTolerance &&
                   MathF.Abs(rotation.Z) < MathConstants.ZeroTolerance &&
                   MathF.Abs(rotation.W - 1f) < MathConstants.ZeroTolerance;
        }

        private void EnsureCoefficientFactorization(bool[] constrained)
        {
            if (_factorizationVersion == _constraintSystemVersion &&
                (_coefficientFactorization is not null || _vertexIndexByUnknown.Length == 0))
            {
                return;
            }

            var vertexCount = _originalPositions.Length;
            var unknownIndexByVertex = Enumerable.Repeat(-1, vertexCount).ToArray();
            var vertexIndexByUnknown = new List<int>(vertexCount);

            for (var index = 0; index < vertexCount; index++)
            {
                if (constrained[index] || _neighbors[index].Length == 0)
                {
                    continue;
                }

                unknownIndexByVertex[index] = vertexIndexByUnknown.Count;
                vertexIndexByUnknown.Add(index);
            }

            var unknownCount = vertexIndexByUnknown.Count;

            _unknownIndexByVertex = unknownIndexByVertex;
            _vertexIndexByUnknown = [.. vertexIndexByUnknown];
            EnsureSolverBuffers(unknownCount);

            if (unknownCount == 0)
            {
                _coefficientFactorization = null;
                _factorizationVersion = _constraintSystemVersion;
                return;
            }

            var entries = new List<(int Row, int Column, double Value)>(unknownCount * 7);

            for (var unknownIndex = 0; unknownIndex < unknownCount; unknownIndex++)
            {
                var index = _vertexIndexByUnknown[unknownIndex];
                entries.Add((unknownIndex, unknownIndex, _neighbors[index].Length));

                foreach (var neighbor in _neighbors[index])
                {
                    var neighborUnknownIndex = unknownIndexByVertex[neighbor];

                    if (neighborUnknownIndex >= 0)
                    {
                        entries.Add((unknownIndex, neighborUnknownIndex, -1d));
                    }
                }
            }

            var coefficientMatrix = CompressedColumnStorage<double>.OfIndexed(unknownCount, unknownCount, entries);
            _coefficientFactorization = SparseCholesky.Create(coefficientMatrix, ColumnOrdering.MinimumDegreeAtPlusA);
            _factorizationVersion = _constraintSystemVersion;
        }

        private void EnsureSolverBuffers(int unknownCount)
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

        private bool TryEnsureCoefficientFactorization(bool[] constrained)
        {
            if (_exactSolveUnavailable)
            {
                return false;
            }

            try
            {
                EnsureCoefficientFactorization(constrained);
                return true;
            }
            catch (InvalidOperationException)
            {
                _exactSolveUnavailable = true;
                _coefficientFactorization = null;
                _previewWeightsDirty = true;
                return false;
            }
            catch (ArgumentException)
            {
                _exactSolveUnavailable = true;
                _coefficientFactorization = null;
                _previewWeightsDirty = true;
                return false;
            }
        }

        private void InvalidateConstrainedSystem()
        {
            _constraintMaskDirty = true;
            _unknownIndexByVertex = [];
            _vertexIndexByUnknown = [];
            _coefficientFactorization = null;
            _previewWeightsDirty = true;
            _exactSolveUnavailable = false;
            _constraintSystemVersion++;
            _factorizationVersion = -1;
        }

        private static Rotation3x3 ExtractRotation(
            double c11,
            double c12,
            double c13,
            double c21,
            double c22,
            double c23,
            double c31,
            double c32,
            double c33)
        {
            var covarianceMagnitude =
                Math.Abs(c11) + Math.Abs(c12) + Math.Abs(c13) +
                Math.Abs(c21) + Math.Abs(c22) + Math.Abs(c23) +
                Math.Abs(c31) + Math.Abs(c32) + Math.Abs(c33);

            if (covarianceMagnitude < 1e-12d)
            {
                return Rotation3x3.Identity;
            }

            var rotation = DoubleQuaternion.Identity;

            for (var iteration = 0; iteration < PolarRotationIterations; iteration++)
            {
                var matrix = Rotation3x3.FromQuaternion(rotation);

                var omegaX =
                    CrossX(matrix.M11, matrix.M21, matrix.M31, c11, c21, c31) +
                    CrossX(matrix.M12, matrix.M22, matrix.M32, c12, c22, c32) +
                    CrossX(matrix.M13, matrix.M23, matrix.M33, c13, c23, c33);
                var omegaY =
                    CrossY(matrix.M11, matrix.M21, matrix.M31, c11, c21, c31) +
                    CrossY(matrix.M12, matrix.M22, matrix.M32, c12, c22, c32) +
                    CrossY(matrix.M13, matrix.M23, matrix.M33, c13, c23, c33);
                var omegaZ =
                    CrossZ(matrix.M11, matrix.M21, matrix.M31, c11, c21, c31) +
                    CrossZ(matrix.M12, matrix.M22, matrix.M32, c12, c22, c32) +
                    CrossZ(matrix.M13, matrix.M23, matrix.M33, c13, c23, c33);

                var denominator = Math.Abs(
                    Dot(matrix.M11, matrix.M21, matrix.M31, c11, c21, c31) +
                    Dot(matrix.M12, matrix.M22, matrix.M32, c12, c22, c32) +
                    Dot(matrix.M13, matrix.M23, matrix.M33, c13, c23, c33)) + 1e-12d;

                omegaX /= denominator;
                omegaY /= denominator;
                omegaZ /= denominator;

                var omegaLength = Math.Sqrt(omegaX * omegaX + omegaY * omegaY + omegaZ * omegaZ);

                if (omegaLength < 1e-9d)
                {
                    break;
                }

                var correction = DoubleQuaternion.FromAxisAngle(
                    omegaX / omegaLength,
                    omegaY / omegaLength,
                    omegaZ / omegaLength,
                    omegaLength);

                rotation = (correction * rotation).Normalized();
            }

            return Rotation3x3.FromQuaternion(rotation);
        }

        private static double Dot(
            double ax,
            double ay,
            double az,
            double bx,
            double by,
            double bz)
        {
            return ax * bx + ay * by + az * bz;
        }

        private static double CrossX(
            double ax,
            double ay,
            double az,
            double bx,
            double by,
            double bz)
        {
            return ay * bz - az * by;
        }

        private static double CrossY(
            double ax,
            double ay,
            double az,
            double bx,
            double by,
            double bz)
        {
            return az * bx - ax * bz;
        }

        private static double CrossZ(
            double ax,
            double ay,
            double az,
            double bx,
            double by,
            double bz)
        {
            return ax * by - ay * bx;
        }

        private bool ShouldUsePreviewSolve(bool[] constrained)
        {
            if (_exactSolveUnavailable)
            {
                return true;
            }

            if (_originalPositions.Length > ExactSolveVertexLimit)
            {
                return true;
            }

            var unknownCount = 0;

            for (var index = 0; index < constrained.Length; index++)
            {
                if (!constrained[index] && _neighbors[index].Length > 0)
                {
                    unknownCount++;
                }
            }

            return unknownCount > ExactSolveUnknownLimit;
        }

        private void EnsurePreviewWeights(bool[] constrained)
        {
            if (!_previewWeightsDirty && _previewWeights.Length == _originalPositions.Length)
            {
                return;
            }

            if (_previewWeights.Length != _originalPositions.Length)
            {
                _previewWeights = new float[_originalPositions.Length];
            }

            Array.Clear(_previewWeights);

            if (_controlVertices.Count == 0)
            {
                _previewWeightsDirty = false;
                return;
            }

            var distances = Enumerable.Repeat(-1, _originalPositions.Length).ToArray();
            var queue = new Queue<int>();

            foreach (var controlVertex in _controlVertices)
            {
                distances[controlVertex] = 0;
                _previewWeights[controlVertex] = 1f;
                queue.Enqueue(controlVertex);
            }

            while (queue.Count > 0)
            {
                var vertex = queue.Dequeue();
                var nextDistance = distances[vertex] + 1;

                if (nextDistance > PreviewGraphDepth)
                {
                    continue;
                }

                foreach (var neighbor in _neighbors[vertex])
                {
                    if (distances[neighbor] >= 0 || (constrained[neighbor] && !_controlVertices.Contains(neighbor)))
                    {
                        continue;
                    }

                    distances[neighbor] = nextDistance;
                    _previewWeights[neighbor] = CalculatePreviewWeight(nextDistance);
                    queue.Enqueue(neighbor);
                }
            }

            _previewWeightsDirty = false;
        }

        private static float CalculatePreviewWeight(int distance)
        {
            var normalizedDistance = Math.Clamp(distance / (float)PreviewGraphDepth, 0f, 1f);
            var smoothFalloff = normalizedDistance * normalizedDistance * (3f - 2f * normalizedDistance);
            return 1f - smoothFalloff;
        }

        private void StabilizeUnconstrainedComponents(bool[] constrained)
        {
            var visited = new bool[_originalPositions.Length];
            var queue = new Queue<int>();

            for (var start = 0; start < _originalPositions.Length; start++)
            {
                if (visited[start])
                {
                    continue;
                }

                var firstVertex = start;
                var hasConstraint = false;

                visited[start] = true;
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    var vertex = queue.Dequeue();
                    hasConstraint |= constrained[vertex];

                    foreach (var neighbor in _neighbors[vertex])
                    {
                        if (visited[neighbor])
                        {
                            continue;
                        }

                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
                }

                if (!hasConstraint)
                {
                    constrained[firstVertex] = true;
                    _constraintPositions[firstVertex] = _originalPositions[firstVertex];
                    _workingPositions[firstVertex] = _originalPositions[firstVertex];
                }
            }
        }

        private static void RecalculateNormals(Mesh mesh)
        {
            if (mesh.Topology != MeshTopology.Triangles)
            {
                return;
            }

            var normals = new Vector3[mesh.Vertices.Length];

            for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
            {
                var index0 = (int)mesh.Indices[index];
                var index1 = (int)mesh.Indices[index + 1];
                var index2 = (int)mesh.Indices[index + 2];

                var p0 = mesh.Vertices[index0].Position;
                var p1 = mesh.Vertices[index1].Position;
                var p2 = mesh.Vertices[index2].Position;

                var normal = Vector3.Cross(p1 - p0, p2 - p0);

                if (normal.LengthSquared < MathConstants.LengthTolerance)
                {
                    continue;
                }

                normals[index0] += normal;
                normals[index1] += normal;
                normals[index2] += normal;
            }

            for (var index = 0; index < mesh.Vertices.Length; index++)
            {
                var normal = normals[index].LengthSquared > MathConstants.LengthTolerance
                    ? normals[index].Normalized()
                    : mesh.Vertices[index].Normal;

                mesh.Vertices[index] = new Vertex(mesh.Vertices[index].Position, normal, mesh.Vertices[index].TexCoords);
            }
        }

        #endregion

        #region Types

        private readonly struct Rotation3x3(
            double m11,
            double m12,
            double m13,
            double m21,
            double m22,
            double m23,
            double m31,
            double m32,
            double m33)
        {
            public static readonly Rotation3x3 Identity = new(
                1d,
                0d,
                0d,
                0d,
                1d,
                0d,
                0d,
                0d,
                1d);

            public double M11 { get; } = m11;
            public double M12 { get; } = m12;
            public double M13 { get; } = m13;
            public double M21 { get; } = m21;
            public double M22 { get; } = m22;
            public double M23 { get; } = m23;
            public double M31 { get; } = m31;
            public double M32 { get; } = m32;
            public double M33 { get; } = m33;

            public static Rotation3x3 FromQuaternion(DoubleQuaternion quaternion)
            {
                var xx = quaternion.X * quaternion.X;
                var yy = quaternion.Y * quaternion.Y;
                var zz = quaternion.Z * quaternion.Z;
                var xy = quaternion.X * quaternion.Y;
                var xz = quaternion.X * quaternion.Z;
                var yz = quaternion.Y * quaternion.Z;
                var wx = quaternion.W * quaternion.X;
                var wy = quaternion.W * quaternion.Y;
                var wz = quaternion.W * quaternion.Z;

                return new Rotation3x3(
                    1d - 2d * (yy + zz),
                    2d * (xy - wz),
                    2d * (xz + wy),
                    2d * (xy + wz),
                    1d - 2d * (xx + zz),
                    2d * (yz - wx),
                    2d * (xz - wy),
                    2d * (yz + wx),
                    1d - 2d * (xx + yy));
            }

            public DoubleVector3 Transform(Vector3 vector)
            {
                return new DoubleVector3(
                    M11 * vector.X + M12 * vector.Y + M13 * vector.Z,
                    M21 * vector.X + M22 * vector.Y + M23 * vector.Z,
                    M31 * vector.X + M32 * vector.Y + M33 * vector.Z);
            }
        }

        private readonly struct DoubleVector3(double x, double y, double z)
        {
            public double X { get; } = x;
            public double Y { get; } = y;
            public double Z { get; } = z;

            public static DoubleVector3 operator +(DoubleVector3 left, DoubleVector3 right)
            {
                return new DoubleVector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
            }

            public static DoubleVector3 operator *(double scalar, DoubleVector3 vector)
            {
                return new DoubleVector3(scalar * vector.X, scalar * vector.Y, scalar * vector.Z);
            }
        }

        private readonly struct DoubleQuaternion(double x, double y, double z, double w)
        {
            public static readonly DoubleQuaternion Identity = new(0d, 0d, 0d, 1d);

            public double X { get; } = x;
            public double Y { get; } = y;
            public double Z { get; } = z;
            public double W { get; } = w;

            public static DoubleQuaternion FromAxisAngle(double axisX, double axisY, double axisZ, double angle)
            {
                var halfAngle = angle * 0.5d;
                var sin = Math.Sin(halfAngle);

                return new DoubleQuaternion(
                    axisX * sin,
                    axisY * sin,
                    axisZ * sin,
                    Math.Cos(halfAngle));
            }

            public DoubleQuaternion Normalized()
            {
                var lengthSquared = X * X + Y * Y + Z * Z + W * W;

                if (lengthSquared < 1e-24d)
                {
                    return Identity;
                }

                var inverseLength = 1d / Math.Sqrt(lengthSquared);
                return new DoubleQuaternion(X * inverseLength, Y * inverseLength, Z * inverseLength, W * inverseLength);
            }

            public static DoubleQuaternion operator *(DoubleQuaternion left, DoubleQuaternion right)
            {
                return new DoubleQuaternion(
                    left.W * right.X + left.X * right.W + left.Y * right.Z - left.Z * right.Y,
                    left.W * right.Y - left.X * right.Z + left.Y * right.W + left.Z * right.X,
                    left.W * right.Z + left.X * right.Y - left.Y * right.X + left.Z * right.W,
                    left.W * right.W - left.X * right.X - left.Y * right.Y - left.Z * right.Z);
            }
        }

        #endregion

    }
}
