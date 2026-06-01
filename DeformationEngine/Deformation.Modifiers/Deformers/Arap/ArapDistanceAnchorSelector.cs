using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers.Arap
{
    internal static class ArapDistanceAnchorSelector
    {
        public static void Select(
            Vector3[] positions,
            IReadOnlySet<int> controlVertices,
            ISet<int> anchorVertices,
            float normalizedDistance)
        {
            anchorVertices.Clear();

            if (controlVertices.Count == 0 || positions.Length == 0)
            {
                return;
            }

            var bounds = AxisAlignedBoundingBox.FromPoints(positions);
            var radius = bounds.Size.Length * normalizedDistance;
            var radiusSquared = radius * radius;

            if (radius <= MathConstants.ZeroTolerance)
            {
                return;
            }

            if (radiusSquared >= bounds.Size.LengthSquared)
            {
                for (var index = 0; index < positions.Length; index++)
                {
                    if (!controlVertices.Contains(index))
                    {
                        anchorVertices.Add(index);
                    }
                }

                return;
            }

            var controlVertexHash = new SpatialHash3<int>(bounds.Min, radius);

            foreach (var controlVertex in controlVertices)
            {
                controlVertexHash.Add(positions[controlVertex], controlVertex);
            }

            for (var index = 0; index < positions.Length; index++)
            {
                if (controlVertices.Contains(index))
                {
                    continue;
                }

                foreach (var controlVertex in controlVertexHash.GetNearby(positions[index]))
                {
                    if ((positions[index] - positions[controlVertex]).LengthSquared > radiusSquared)
                    {
                        continue;
                    }

                    anchorVertices.Add(index);
                    break;
                }
            }
        }
    }
}
