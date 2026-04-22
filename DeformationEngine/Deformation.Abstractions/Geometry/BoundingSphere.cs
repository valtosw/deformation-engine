using OpenTK.Mathematics;

namespace Deformation.Abstractions.Geometry
{
    public sealed record BoundingSphere(Vector3 Center, float Radius)
    {
        public static BoundingSphere FromAxisAlignedBoundingBox(AxisAlignedBoundingBox axisAlignedBoundingBox)
        {
            var radius = (axisAlignedBoundingBox.Max - axisAlignedBoundingBox.Center).Length;
            return new BoundingSphere(axisAlignedBoundingBox.Center, radius);
        }
    }
}
