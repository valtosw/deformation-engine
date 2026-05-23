using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Geometry;
using Deformation.Abstractions.Math;
using OpenTK.Mathematics;
using Plane = Deformation.Abstractions.Math.Plane;

namespace Deformation.Abstractions.Extensions
{
    public static class RayExtensions
    {
        #region Public Logic

        public static bool Intersects(this Ray ray, AxisAlignedBoundingBox box, out float distance)
        {
            distance = 0f;

            var tmin = (box.Min.X - ray.Origin.X) / ray.Direction.X;
            var tmax = (box.Max.X - ray.Origin.X) / ray.Direction.X;

            if (tmin > tmax)
            {
                (tmax, tmin) = (tmin, tmax);
            }

            var tymin = (box.Min.Y - ray.Origin.Y) / ray.Direction.Y;
            var tymax = (box.Max.Y - ray.Origin.Y) / ray.Direction.Y;

            if (tymin > tymax)
            {
                (tymax, tymin) = (tymin, tymax);
            }

            if (tmin > tymax || tymin > tmax)
            {
                return false;
            }

            if (tymin > tmin)
            {
                tmin = tymin;
            }

            if (tymax < tmax)
            {
                tmax = tymax;
            }

            var tzmin = (box.Min.Z - ray.Origin.Z) / ray.Direction.Z;
            var tzmax = (box.Max.Z - ray.Origin.Z) / ray.Direction.Z;

            if (tzmin > tzmax)
            {
                (tzmax, tzmin) = (tzmin, tzmax);
            }

            if (tmin > tzmax || tzmin > tmax)
            {
                return false;
            }

            if (tzmin > tmin)
            {
                tmin = tzmin;
            }

            if (tzmax < tmax)
            {
                tmax = tzmax;
            }

            distance = tmin;

            return distance >= 0f;
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

            var parameter = Vector3.Dot(plane.Point - ray.Origin, plane.Normal) / denominator;

            if (parameter < 0f)
            {
                return null;
            }

            return ray.Origin + ray.Direction * parameter;
        }

        #endregion
    }
}