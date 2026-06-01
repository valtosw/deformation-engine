using Deformation.Abstractions.Comparers;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers
{
    public sealed class ArapDeformer : IDeformer
    {
        #region Fields

        private readonly HashSet<int> _controlVertices = [];
        private readonly HashSet<int> _manualAnchorVertices = [];
        private readonly HashSet<int> _distanceAnchorVertices = [];

        private Vector3[] _originalPositions = [];
        private Vector3[] _workingPositions = [];
        private Vector3[] _constraintPositions = [];
        private int[][] _neighbors = [];
        private int[][] _weldedVertexGroups = [];
        private Edge[] _edges = [];

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
        public int VertexCount => _originalPositions.Length;

        #endregion

        #region Public Logic

        public void Initialize(Mesh originalMesh)
        {
            _originalPositions = originalMesh.Vertices.Select(vertex => vertex.Position).ToArray();
            _workingPositions = new Vector3[_originalPositions.Length];
            _constraintPositions = new Vector3[_originalPositions.Length];

            BuildTopology(originalMesh);

            _controlVertices.Clear();
            _manualAnchorVertices.Clear();
            _distanceAnchorVertices.Clear();
            _pivot = originalMesh.LocalBoundingBox.Center;
            _handlePosition = _pivot;
            _handleRotation = Quaternion.Identity;
            _distanceAnchorsDirty = true;
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
            _neighbors = [];
            _weldedVertexGroups = [];
            _edges = [];
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
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            DeformationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetActionMode(ArapActionMode actionMode)
        {
            ActionMode = actionMode;
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

        public void BeginDeform()
        {
            RecalculatePivot();
            _handlePosition = _pivot;
            _handleRotation = Quaternion.Identity;
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

            EnsureDistanceAnchors();
            SolveConstrainedShape();

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

            var edges = new Dictionary<(int A, int B), float>();

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
            _edges = edges.Select(pair => new Edge(pair.Key.A, pair.Key.B, pair.Value)).ToArray();

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

                var edge = indexA < indexB ? (indexA, indexB) : (indexB, indexA);

                if (!edges.ContainsKey(edge))
                {
                    edges[edge] = (_originalPositions[edge.Item1] - _originalPositions[edge.Item2]).Length;
                }
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

            for (var index = 0; index < _originalPositions.Length; index++)
            {
                if (_controlVertices.Contains(index))
                {
                    continue;
                }

                foreach (var controlVertex in _controlVertices)
                {
                    if ((_originalPositions[index] - _originalPositions[controlVertex]).LengthSquared <= radiusSquared)
                    {
                        _distanceAnchorVertices.Add(index);
                        break;
                    }
                }
            }

            _distanceAnchorsDirty = false;
        }

        private void SolveConstrainedShape()
        {
            var constrained = new bool[_originalPositions.Length];
            _originalPositions.CopyTo(_workingPositions, 0);

            foreach (var controlVertex in _controlVertices)
            {
                constrained[controlVertex] = true;
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

            var harmonicPassCount = Math.Max(1, Iterations * 2);

            for (var pass = 0; pass < harmonicPassCount; pass++)
            {
                for (var index = 0; index < _workingPositions.Length; index++)
                {
                    if (constrained[index] || _neighbors[index].Length == 0)
                    {
                        continue;
                    }

                    var average = Vector3.Zero;

                    foreach (var neighbor in _neighbors[index])
                    {
                        average += _workingPositions[neighbor];
                    }

                    _workingPositions[index] = average / _neighbors[index].Length;
                }
            }

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                ProjectEdgeLengths(constrained);
                RelaxInterior(constrained, 0.35f);
                ApplyConstraints(constrained);
            }
        }

        private Vector3 TransformControlPoint(Vector3 position)
        {
            var offset = position - _pivot;
            return _handlePosition + Vector3.Transform(offset, _handleRotation);
        }

        private void ProjectEdgeLengths(bool[] constrained)
        {
            foreach (var edge in _edges)
            {
                var delta = _workingPositions[edge.IndexB] - _workingPositions[edge.IndexA];
                var length = delta.Length;

                if (length < MathConstants.LengthTolerance)
                {
                    continue;
                }

                var correction = delta * ((length - edge.RestLength) / length);
                var aConstrained = constrained[edge.IndexA];
                var bConstrained = constrained[edge.IndexB];

                if (aConstrained && bConstrained)
                {
                    continue;
                }

                if (aConstrained)
                {
                    _workingPositions[edge.IndexB] -= correction;
                }
                else if (bConstrained)
                {
                    _workingPositions[edge.IndexA] += correction;
                }
                else
                {
                    _workingPositions[edge.IndexA] += correction * 0.5f;
                    _workingPositions[edge.IndexB] -= correction * 0.5f;
                }
            }
        }

        private void RelaxInterior(bool[] constrained, float blend)
        {
            for (var index = 0; index < _workingPositions.Length; index++)
            {
                if (constrained[index] || _neighbors[index].Length == 0)
                {
                    continue;
                }

                var average = Vector3.Zero;

                foreach (var neighbor in _neighbors[index])
                {
                    average += _workingPositions[neighbor];
                }

                average /= _neighbors[index].Length;
                _workingPositions[index] = Vector3.Lerp(_workingPositions[index], average, blend);
            }
        }

        private void ApplyConstraints(bool[] constrained)
        {
            for (var index = 0; index < constrained.Length; index++)
            {
                if (constrained[index])
                {
                    _workingPositions[index] = _constraintPositions[index];
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

        #region Nested Types

        private readonly record struct Edge(int IndexA, int IndexB, float RestLength);

        #endregion
    }
}
