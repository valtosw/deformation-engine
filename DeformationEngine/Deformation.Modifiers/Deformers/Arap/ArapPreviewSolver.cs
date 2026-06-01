using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers.Arap
{
    internal sealed class ArapPreviewSolver
    {
        private const int PreviewGraphDepth = 48;

        private float[] _weights = [];
        private bool _isDirty = true;

        public void Clear()
        {
            _weights = [];
            _isDirty = true;
        }

        public void Invalidate()
        {
            _isDirty = true;
        }

        public void Solve(
            Vector3[] originalPositions,
            Vector3[] workingPositions,
            int[][] neighbors,
            bool[] constrained,
            IReadOnlySet<int> controlVertices,
            Func<Vector3, Vector3> transformControlPoint)
        {
            EnsureWeights(originalPositions.Length, neighbors, constrained, controlVertices);

            for (var index = 0; index < originalPositions.Length; index++)
            {
                var weight = _weights[index];

                if (weight <= MathConstants.ZeroTolerance)
                {
                    workingPositions[index] = originalPositions[index];
                    continue;
                }

                if (constrained[index] && !controlVertices.Contains(index))
                {
                    workingPositions[index] = originalPositions[index];
                    continue;
                }

                var targetPosition = transformControlPoint(originalPositions[index]);
                workingPositions[index] = Vector3.Lerp(originalPositions[index], targetPosition, weight);
            }
        }

        public void Apply(Mesh mesh, Vector3[] workingPositions)
        {
            for (var index = 0; index < mesh.Vertices.Length; index++)
            {
                if (_weights[index] <= MathConstants.ZeroTolerance)
                {
                    continue;
                }

                mesh.Vertices[index] = new Vertex(workingPositions[index], mesh.Vertices[index].Normal, mesh.Vertices[index].TexCoords);
            }
        }

        private void EnsureWeights(
            int vertexCount,
            int[][] neighbors,
            bool[] constrained,
            IReadOnlySet<int> controlVertices)
        {
            if (!_isDirty && _weights.Length == vertexCount)
            {
                return;
            }

            if (_weights.Length != vertexCount)
            {
                _weights = new float[vertexCount];
            }

            Array.Clear(_weights);

            if (controlVertices.Count == 0)
            {
                _isDirty = false;
                return;
            }

            var distances = Enumerable.Repeat(-1, vertexCount).ToArray();
            var queue = new Queue<int>();

            foreach (var controlVertex in controlVertices)
            {
                distances[controlVertex] = 0;
                _weights[controlVertex] = 1f;
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

                foreach (var neighbor in neighbors[vertex])
                {
                    if (distances[neighbor] >= 0 || (constrained[neighbor] && !controlVertices.Contains(neighbor)))
                    {
                        continue;
                    }

                    distances[neighbor] = nextDistance;
                    _weights[neighbor] = CalculateWeight(nextDistance);
                    queue.Enqueue(neighbor);
                }
            }

            _isDirty = false;
        }

        private static float CalculateWeight(int distance)
        {
            var normalizedDistance = Math.Clamp(distance / (float)PreviewGraphDepth, 0f, 1f);
            var smoothFalloff = normalizedDistance * normalizedDistance * (3f - 2f * normalizedDistance);
            return 1f - smoothFalloff;
        }
    }
}
