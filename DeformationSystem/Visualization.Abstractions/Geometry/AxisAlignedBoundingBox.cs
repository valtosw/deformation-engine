using OpenTK.Mathematics;

namespace Visualization.Abstractions.Geometry
{
    public sealed record AxisAlignedBoundingBox(Vector3 Min, Vector3 Max)
    {
        public Vector3 Center { get; } = (Min + Max) * 0.5f;
        public Vector3 Size { get; } = Max - Min;

        public static AxisAlignedBoundingBox FromPoints(IEnumerable<Vector3> points)
        {
            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);
            var hasPoints = false;

            foreach (var point in points)
            {
                min = Vector3.ComponentMin(min, point);
                max = Vector3.ComponentMax(max, point);
                hasPoints = true;
            }

            return hasPoints ? new AxisAlignedBoundingBox(min, max) : new AxisAlignedBoundingBox(Vector3.Zero, Vector3.Zero);
        }

        public static AxisAlignedBoundingBox Combine(AxisAlignedBoundingBox? a, AxisAlignedBoundingBox? b)
        {
            if (a is null)
                return b ?? new AxisAlignedBoundingBox(Vector3.Zero, Vector3.Zero);

            if (b is null)
                return a;

            return new AxisAlignedBoundingBox(
                Vector3.ComponentMin(a.Min, b.Min),
                Vector3.ComponentMax(a.Max, b.Max)
            );
        }
    }
}
