using OpenTK.Mathematics;
using System.Linq;

namespace Deformation.Modifiers.Deformers.Arap
{
    internal sealed class ArapConstraintMask
    {
        #region Fields

        private bool[] _mask = [];
        private bool _isDirty = true;
        private int _version;

        #endregion

        #region Properties

        public int Version => _version;

        #endregion

        #region Public Logic

        public void Clear()
        {
            _mask = [];
            _isDirty = true;
            _version = 0;
        }

        public void Invalidate()
        {
            _isDirty = true;
            _version++;
        }

        public bool[] Get(
            int vertexCount,
            int[][] neighbors,
            IReadOnlySet<int> controlVertices,
            IReadOnlySet<int> anchorVertices,
            Vector3[] originalPositions,
            Vector3[] workingPositions,
            Vector3[] constraintPositions)
        {
            if (!_isDirty && _mask.Length == vertexCount)
            {
                return _mask;
            }

            if (_mask.Length != vertexCount)
            {
                _mask = new bool[vertexCount];
            }
            else
            {
                Array.Clear(_mask);
            }

            foreach (var controlVertex in controlVertices)
            {
                _mask[controlVertex] = true;
            }

            foreach (var anchorVertex in anchorVertices.Where(anchorVertex => !controlVertices.Contains(anchorVertex)))
            {
                _mask[anchorVertex] = true;
            }

            StabilizeUnconstrainedComponents(neighbors, originalPositions, workingPositions, constraintPositions);
            _isDirty = false;

            return _mask;
        }

        #endregion

        #region Private Logic

        private void StabilizeUnconstrainedComponents(
            int[][] neighbors,
            Vector3[] originalPositions,
            Vector3[] workingPositions,
            Vector3[] constraintPositions)
        {
            var visited = new bool[_mask.Length];
            var queue = new Queue<int>();

            for (var start = 0; start < _mask.Length; start++)
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
                    hasConstraint |= _mask[vertex];

                    foreach (var neighbor in neighbors[vertex])
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
                    _mask[firstVertex] = true;
                    constraintPositions[firstVertex] = originalPositions[firstVertex];
                    workingPositions[firstVertex] = originalPositions[firstVertex];
                }
            }
        }

        #endregion
    }
}