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
        #region Fields

        private readonly ArapSelection _selection = new();
        private readonly ArapSolverCoordinator _coordinator = new();
        private readonly ArapConstraintMask _constraintMask = new();

        private Vector3[] _originalPositions = [];
        private Vector3[] _workingPositions = [];
        private Vector3[] _constraintPositions = [];
        private ArapTopology? _topology;

        private Vector3 _handlePosition = Vector3.Zero;
        private Quaternion _handleRotation = Quaternion.Identity;
        private bool _isInitialized;

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

        public IReadOnlySet<int> ControlVertices => _selection.ControlVertices;
        public IReadOnlySet<int> AnchorVertices => _selection.GetAnchorVertices(AnchorType, _originalPositions, AnchorDistance);

        public Vector3 Pivot => _selection.Pivot;
        public bool IsInitialized => _isInitialized;
        public bool HasSelection => _selection.HasSelection;
        public bool HasChanges => _selection.ControlVertices.Count > 0 && (_handlePosition != Pivot || _handleRotation != Quaternion.Identity);
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

            _selection.Clear();
            _selection.RecalculatePivot(_originalPositions);
            _handlePosition = Pivot;
            _handleRotation = Quaternion.Identity;

            _constraintMask.Clear();
            _coordinator.Initialize(_originalPositions.Length);
            _isInitialized = true;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            _selection.Clear();
            _originalPositions = [];
            _workingPositions = [];
            _constraintPositions = [];
            _topology = null;

            _constraintMask.Clear();
            _coordinator.Clear();

            _handlePosition = Vector3.Zero;
            _handleRotation = Quaternion.Identity;
            _isInitialized = false;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Reset()
        {
            _selection.Clear();
            _handlePosition = Vector3.Zero;
            _handleRotation = Quaternion.Identity;

            InvalidateSolvers();

            SelectionChanged?.Invoke(this, EventArgs.Empty);
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetAnchorType(ArapAnchorType anchorType)
        {
            if (AnchorType == anchorType)
            {
                return;
            }

            AnchorType = anchorType;
            _selection.MarkDistanceAnchorsDirty();
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
            _selection.MarkDistanceAnchorsDirty();
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

            var hasChanged = _selection.PaintVertices(vertexIndices, erase, ActionMode, AnchorType, _topology.WeldedVertexGroups);

            if (hasChanged)
            {
                _selection.RecalculatePivot(_originalPositions);
                _handlePosition = Pivot;
                _handleRotation = Quaternion.Identity;

                InvalidateSolvers();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                DeformationChanged?.Invoke(this, EventArgs.Empty);
            }
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
            if (vertexIndex >= 0 && vertexIndex < _originalPositions.Length)
            {
                return _originalPositions[vertexIndex];
            }

            return Vector3.Zero;
        }

        public Vector3 GetCurrentPosition(int vertexIndex)
        {
            if (vertexIndex < 0 || vertexIndex >= _originalPositions.Length)
            {
                return Vector3.Zero;
            }

            if (ActionMode == ArapActionMode.Deform && ControlVertices.Contains(vertexIndex))
            {
                return TransformControlPoint(_originalPositions[vertexIndex]);
            }

            if (HasChanges && vertexIndex < _workingPositions.Length)
            {
                return _workingPositions[vertexIndex];
            }

            return _originalPositions[vertexIndex];
        }

        public void BeginDeform()
        {
            _selection.RecalculatePivot(_originalPositions);
            _handlePosition = Pivot;
            _handleRotation = Quaternion.Identity;

            if (_topology is null)
            {
                return;
            }

            var context = CreateSolverContext();
            var solver = _coordinator.SelectSolver(context);

            if (solver is ArapPreviewSolver)
            {
                solver.Solve(context);
            }
            else
            {
                solver.TryPrepare(context);
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
                ControlVertices.Count == 0 ||
                ActionMode != ArapActionMode.Deform ||
                !HasChanges)
            {
                return;
            }

            var context = CreateSolverContext();
            var solver = _coordinator.SelectSolver(context);

            solver.Solve(context);
            solver.ApplyDeformation(mesh, context);

            mesh.RecalculateNormals();
        }

        #endregion

        #region Private Logic

        private ArapSolverContext CreateSolverContext()
        {
            return new ArapSolverContext
            {
                OriginalPositions = _originalPositions,
                WorkingPositions = _workingPositions,
                ConstraintPositions = _constraintPositions,
                Neighbors = _topology?.Neighbors ?? [],
                Constrained = GetConstraintMask(),
                ConstraintVersion = _constraintMask.Version,
                Iterations = Iterations,
                UseIdentityRotations = IsIdentityRotation(_handleRotation),
                ControlVertices = _selection.ControlVertices,
                TransformControlPoint = TransformControlPoint
            };
        }

        private bool[] GetConstraintMask()
        {
            if (_topology is null)
            {
                return [];
            }

            return _constraintMask.Get(
                _originalPositions.Length,
                _topology.Neighbors,
                _selection.ControlVertices,
                AnchorVertices,
                _originalPositions,
                _workingPositions,
                _constraintPositions);
        }

        private Vector3 TransformControlPoint(Vector3 position)
        {
            var offset = position - Pivot;

            return _handlePosition + Vector3.Transform(offset, _handleRotation);
        }

        private void InvalidateSolvers()
        {
            _constraintMask.Invalidate();
            _coordinator.Invalidate();
        }

        private static bool IsIdentityRotation(Quaternion rotation)
        {
            var isIdentityX = MathF.Abs(rotation.X) < MathConstants.ZeroTolerance;
            var isIdentityY = MathF.Abs(rotation.Y) < MathConstants.ZeroTolerance;
            var isIdentityZ = MathF.Abs(rotation.Z) < MathConstants.ZeroTolerance;
            var isIdentityW = MathF.Abs(rotation.W - 1f) < MathConstants.ZeroTolerance;

            return isIdentityX && isIdentityY && isIdentityZ && isIdentityW;
        }

        #endregion
    }
}