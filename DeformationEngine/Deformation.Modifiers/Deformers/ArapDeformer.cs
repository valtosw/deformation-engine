using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using Deformation.Modifiers.Deformers.Arap;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers
{
    public sealed class ArapDeformer : IDeformer
    {
        #region Constants

        private const int ExactSolveVertexLimit = 75000;
        private const int ExactSolveUnknownLimit = 60000;

        #endregion

        #region Fields

        private readonly HashSet<int> _controlVertices = [];
        private readonly HashSet<int> _manualAnchorVertices = [];
        private readonly HashSet<int> _distanceAnchorVertices = [];
        private readonly ArapConstraintMask _constraintMask = new();
        private readonly ArapExactSolver _exactSolver = new();
        private readonly ArapPreviewSolver _previewSolver = new();

        private Vector3[] _originalPositions = [];
        private Vector3[] _workingPositions = [];
        private Vector3[] _constraintPositions = [];
        private ArapTopology? _topology;

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
            _topology = ArapTopology.Build(originalMesh);

            _controlVertices.Clear();
            _manualAnchorVertices.Clear();
            _distanceAnchorVertices.Clear();
            _pivot = originalMesh.LocalBoundingBox.Center;
            _handlePosition = _pivot;
            _handleRotation = Quaternion.Identity;
            _distanceAnchorsDirty = true;
            _constraintMask.Clear();
            _exactSolver.Reset(_originalPositions.Length);
            _previewSolver.Clear();
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
            _topology = null;
            _constraintMask.Clear();
            _exactSolver.Clear();
            _previewSolver.Clear();
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
            InvalidateSolvers();
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
            InvalidateSolvers();
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
            if (!_isInitialized || _topology is null)
            {
                return;
            }

            var changed = false;

            foreach (var vertexIndex in ExpandWeldedVertices(vertexIndices, _topology.WeldedVertexGroups))
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
            InvalidateSolvers();
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

            if (_topology is null)
            {
                return;
            }

            var constrained = GetConstraintMask();

            if (ShouldUsePreviewSolve(constrained))
            {
                _previewSolver.Solve(_originalPositions, _workingPositions, _topology.Neighbors, constrained, _controlVertices, TransformControlPoint);
            }
            else
            {
                _exactSolver.TryPrepare(constrained, _topology.Neighbors, _constraintMask.Version);
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
            if (!_isInitialized ||
                _topology is null ||
                _originalPositions.Length != mesh.Vertices.Length ||
                _controlVertices.Count == 0 ||
                ActionMode != ArapActionMode.Deform ||
                !HasChanges)
            {
                return;
            }

            EnsureDistanceAnchors();
            var constrained = GetConstraintMask();

            if (ShouldUsePreviewSolve(constrained) || !_exactSolver.TryPrepare(constrained, _topology.Neighbors, _constraintMask.Version))
            {
                _previewSolver.Solve(_originalPositions, _workingPositions, _topology.Neighbors, constrained, _controlVertices, TransformControlPoint);
                _previewSolver.Apply(mesh, _workingPositions);
                return;
            }

            SolveExact(constrained, _topology.Neighbors);
            ApplyWorkingPositions(mesh);
            mesh.RecalculateNormals();
        }

        #endregion

        #region Private Logic

        private bool[] GetConstraintMask()
        {
            if (_topology is null)
            {
                return [];
            }

            return _constraintMask.Get(
                _originalPositions.Length,
                _topology.Neighbors,
                _controlVertices,
                AnchorVertices,
                _originalPositions,
                _workingPositions,
                _constraintPositions);
        }

        private void SolveExact(bool[] constrained, int[][] neighbors)
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

                _constraintPositions[anchorVertex] = _originalPositions[anchorVertex];
                _workingPositions[anchorVertex] = _constraintPositions[anchorVertex];
            }

            _exactSolver.Solve(
                _originalPositions,
                _workingPositions,
                _constraintPositions,
                neighbors,
                constrained,
                _constraintMask.Version,
                Iterations,
                IsIdentityRotation(_handleRotation));
        }

        private void ApplyWorkingPositions(Mesh mesh)
        {
            for (var index = 0; index < mesh.Vertices.Length; index++)
            {
                mesh.Vertices[index] = new Vertex(_workingPositions[index], mesh.Vertices[index].Normal, mesh.Vertices[index].TexCoords);
            }
        }

        private void EnsureDistanceAnchors()
        {
            if (AnchorType != ArapAnchorType.Distance || !_distanceAnchorsDirty)
            {
                return;
            }

            ArapDistanceAnchorSelector.Select(_originalPositions, _controlVertices, _distanceAnchorVertices, AnchorDistance);
            _distanceAnchorsDirty = false;
        }

        private IEnumerable<int> ExpandWeldedVertices(IEnumerable<int> vertexIndices, int[][] weldedVertexGroups)
        {
            foreach (var vertexIndex in vertexIndices)
            {
                if (vertexIndex < 0 || vertexIndex >= weldedVertexGroups.Length)
                {
                    continue;
                }

                foreach (var weldedIndex in weldedVertexGroups[vertexIndex])
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

        private Vector3 TransformControlPoint(Vector3 position)
        {
            var offset = position - _pivot;
            return _handlePosition + Vector3.Transform(offset, _handleRotation);
        }

        private bool ShouldUsePreviewSolve(bool[] constrained)
        {
            if (_exactSolver.IsUnavailable || _originalPositions.Length > ExactSolveVertexLimit)
            {
                return true;
            }

            var unknownCount = 0;

            for (var index = 0; index < constrained.Length; index++)
            {
                if (!constrained[index] && _topology?.Neighbors[index].Length > 0)
                {
                    unknownCount++;
                }
            }

            return unknownCount > ExactSolveUnknownLimit;
        }

        private void InvalidateSolvers()
        {
            _constraintMask.Invalidate();
            _exactSolver.Invalidate();
            _previewSolver.Invalidate();
        }

        private static bool IsIdentityRotation(Quaternion rotation)
        {
            return MathF.Abs(rotation.X) < MathConstants.ZeroTolerance &&
                   MathF.Abs(rotation.Y) < MathConstants.ZeroTolerance &&
                   MathF.Abs(rotation.Z) < MathConstants.ZeroTolerance &&
                   MathF.Abs(rotation.W - 1f) < MathConstants.ZeroTolerance;
        }

        #endregion
    }
}
