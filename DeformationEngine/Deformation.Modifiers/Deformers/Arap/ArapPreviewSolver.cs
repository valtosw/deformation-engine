using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers.Arap
{
    internal sealed class ArapPreviewSolver : IArapSolver
    {
        #region Fields

        private const int PreviewGraphDepth = 48;

        private float[] _weights = [];
        private bool _isDirty = true;

        #endregion

        #region Properties

        public bool IsUnavailable => false;

        #endregion

        #region Public Logic

        public void Clear()
        {
            _weights = [];
            _isDirty = true;
        }

        public void Invalidate()
        {
            _isDirty = true;
        }

        public bool TryPrepare(ArapSolverContext context)
        {
            return true;
        }

        public void Solve(ArapSolverContext context)
        {
            EnsureWeights(context.OriginalPositions.Length, context.Neighbors, context.Constrained, context.ControlVertices);

            for (var index = 0; index < context.OriginalPositions.Length; index++)
            {
                var weight = _weights[index];

                if (weight <= MathConstants.ZeroTolerance)
                {
                    context.WorkingPositions[index] = context.OriginalPositions[index];
                    continue;
                }

                if (context.Constrained[index] && !context.ControlVertices.Contains(index))
                {
                    context.WorkingPositions[index] = context.OriginalPositions[index];
                    continue;
                }

                var targetPosition = context.TransformControlPoint(context.OriginalPositions[index]);
                context.WorkingPositions[index] = Vector3.Lerp(context.OriginalPositions[index], targetPosition, weight);
            }
        }

        public void ApplyDeformation(Mesh mesh, ArapSolverContext context)
        {
            for (var index = 0; index < mesh.Vertices.Length; index++)
            {
                var weight = _weights[index];

                if (weight <= MathConstants.ZeroTolerance)
                {
                    continue;
                }

                var position = context.WorkingPositions[index];
                var normal = mesh.Vertices[index].Normal;
                var textureCoordinates = mesh.Vertices[index].TexCoords;

                mesh.Vertices[index] = new Vertex(position, normal, textureCoordinates);
            }
        }

        #endregion

        #region Private Logic

        private void EnsureWeights(
            int vertexCount,
            int[][] neighbors,
            bool[] constrained,
            IReadOnlySet<int> controlVertices)
        {
            var isUpToDate = !_isDirty && _weights.Length == vertexCount;

            if (isUpToDate)
            {
                return;
            }

            if (_weights.Length != vertexCount)
            {
                _weights = new float[vertexCount];
            }
            else
            {
                Array.Clear(_weights);
            }

            if (controlVertices.Count == 0)
            {
                _isDirty = false;
                return;
            }

            var distances = new int[vertexCount];
            Array.Fill(distances, -1);

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
                    var isSkipConditionMet = distances[neighbor] >= 0 || (constrained[neighbor] && !controlVertices.Contains(neighbor));

                    if (isSkipConditionMet)
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

        #endregion
    }
}