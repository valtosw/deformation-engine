using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Geometry;
using Deformation.Abstractions.Math;
using OpenTK.Mathematics;
using Plane = Deformation.Abstractions.Math.Plane;

namespace Deformation.Abstractions.Extensions
{
    public static class RayExtensions
    {
        public static bool Intersects(this Ray ray, AxisAlignedBoundingBox box, out float distance)
        {
            distance = 0f;

            var intersection1 = (box.Min.X - ray.Origin.X) / ray.Direction.X;
            var intersection2 = (box.Max.X - ray.Origin.X) / ray.Direction.X;

            var minimumDistance = MathF.Min(intersection1, intersection2);
            var maximumDistance = MathF.Max(intersection1, intersection2);

            intersection1 = (box.Min.Y - ray.Origin.Y) / ray.Direction.Y;
            intersection2 = (box.Max.Y - ray.Origin.Y) / ray.Direction.Y;

            minimumDistance = MathF.Max(minimumDistance, MathF.Min(intersection1, intersection2));
            maximumDistance = MathF.Min(maximumDistance, MathF.Max(intersection1, intersection2));

            intersection1 = (box.Min.Z - ray.Origin.Z) / ray.Direction.Z;
            intersection2 = (box.Max.Z - ray.Origin.Z) / ray.Direction.Z;

            minimumDistance = MathF.Max(minimumDistance, MathF.Min(intersection1, intersection2));
            maximumDistance = MathF.Min(maximumDistance, MathF.Max(intersection1, intersection2));

            if (maximumDistance >= minimumDistance && maximumDistance >= MathConstants.ZeroTolerance)
            {
                distance = minimumDistance >= MathConstants.ZeroTolerance ? minimumDistance : maximumDistance;
                return true;
            }

            return false;
        }

        public static Ray Transformed(this Ray ray, Matrix4 matrix)
        {
            var origin = matrix.TransformPoint(ray.Origin);
            var direction = matrix.TransformDirection(ray.Direction).Normalized();

            return new Ray(origin, direction);
        }

        public static Vector3? Intersects(this Ray ray, Plane plane)
        {
            var denominator = Vector3.Dot(plane.Normal, ray.Direction);

            if (System.Math.Abs(denominator) < MathConstants.ZeroTolerance)
            {
                return null;
            }

            var intersectionDistance = Vector3.Dot(plane.Point - ray.Origin, plane.Normal) / denominator;

            if (intersectionDistance < MathConstants.ZeroTolerance)
            {
                return null;
            }

            return ray.Origin + ray.Direction * intersectionDistance;
        }
    }
}