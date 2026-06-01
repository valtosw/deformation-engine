using System.Collections.Generic;
using Deformation.Abstractions.Enums;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers.Arap
{
    internal sealed class ArapSelection
    {
        #region Fields

        private readonly HashSet<int> _controlVertices = [];
        private readonly HashSet<int> _manualAnchorVertices = [];
        private readonly HashSet<int> _distanceAnchorVertices = [];

        private bool _distanceAnchorsDirty = true;
        private Vector3 _pivot = Vector3.Zero;

        #endregion

        #region Properties

        public IReadOnlySet<int> ControlVertices => _controlVertices;
        public IReadOnlySet<int> ManualAnchorVertices => _manualAnchorVertices;
        public IReadOnlySet<int> DistanceAnchorVertices => _distanceAnchorVertices;

        public Vector3 Pivot => _pivot;
        public bool HasSelection => _controlVertices.Count > 0 || _manualAnchorVertices.Count > 0 || _distanceAnchorVertices.Count > 0;

        #endregion

        #region Public Logic

        public void Clear()
        {
            _controlVertices.Clear();
            _manualAnchorVertices.Clear();
            _distanceAnchorVertices.Clear();
            _distanceAnchorsDirty = true;
            _pivot = Vector3.Zero;
        }

        public void MarkDistanceAnchorsDirty()
        {
            _distanceAnchorsDirty = true;
        }

        public IReadOnlySet<int> GetAnchorVertices(ArapAnchorType anchorType, Vector3[] originalPositions, float anchorDistance)
        {
            if (anchorType == ArapAnchorType.Distance)
            {
                EnsureDistanceAnchors(originalPositions, anchorDistance);
                return _distanceAnchorVertices;
            }

            return _manualAnchorVertices;
        }

        public bool PaintVertices(
            IEnumerable<int> vertexIndices,
            bool erase,
            ArapActionMode actionMode,
            ArapAnchorType anchorType,
            int[][] weldedVertexGroups)
        {
            var hasChanged = false;
            var expandedVertices = ExpandWeldedVertices(vertexIndices, weldedVertexGroups);

            foreach (var vertexIndex in expandedVertices)
            {
                if (actionMode == ArapActionMode.ControlPoints)
                {
                    var isSelectionChanged = erase ? _controlVertices.Remove(vertexIndex) : _controlVertices.Add(vertexIndex);

                    if (isSelectionChanged)
                    {
                        hasChanged = true;
                    }

                    if (!erase)
                    {
                        _manualAnchorVertices.Remove(vertexIndex);
                    }
                }
                else if (actionMode == ArapActionMode.AnchorPoints && anchorType == ArapAnchorType.Manual)
                {
                    var isSelectionChanged = erase ? _manualAnchorVertices.Remove(vertexIndex) : _manualAnchorVertices.Add(vertexIndex);

                    if (isSelectionChanged)
                    {
                        hasChanged = true;
                    }

                    if (!erase)
                    {
                        _controlVertices.Remove(vertexIndex);
                    }
                }
            }

            if (hasChanged)
            {
                _distanceAnchorsDirty = true;
            }

            return hasChanged;
        }

        public void RecalculatePivot(Vector3[] originalPositions)
        {
            if (_controlVertices.Count == 0)
            {
                return;
            }

            var pivot = Vector3.Zero;

            foreach (var vertexIndex in _controlVertices)
            {
                pivot += originalPositions[vertexIndex];
            }

            _pivot = pivot / _controlVertices.Count;
        }

        #endregion

        #region Private Logic

        private void EnsureDistanceAnchors(Vector3[] originalPositions, float anchorDistance)
        {
            if (!_distanceAnchorsDirty)
            {
                return;
            }

            ArapDistanceAnchorSelector.Select(originalPositions, _controlVertices, _distanceAnchorVertices, anchorDistance);
            _distanceAnchorsDirty = false;
        }

        private static IEnumerable<int> ExpandWeldedVertices(IEnumerable<int> vertexIndices, int[][] weldedVertexGroups)
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

        #endregion
    }
}