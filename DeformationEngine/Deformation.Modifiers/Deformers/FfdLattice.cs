using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers
{
    public sealed class FfdLattice
    {
        #region Fields

        private readonly Vector3[] _restControlPoints;
        private readonly Vector3[] _controlPoints;

        #endregion

        #region Constructors

        public FfdLattice(AxisAlignedBoundingBox sourceBounds, int resolutionX, int resolutionY, int resolutionZ)
        {
            ResolutionX = Math.Max(2, resolutionX);
            ResolutionY = Math.Max(2, resolutionY);
            ResolutionZ = Math.Max(2, resolutionZ);
            Bounds = CreateUsableBounds(sourceBounds);

            _restControlPoints = new Vector3[ControlPointCount];
            _controlPoints = new Vector3[ControlPointCount];

            InitializeControlPoints();
        }

        #endregion

        #region Properties

        public AxisAlignedBoundingBox Bounds { get; }
        public int ResolutionX { get; }
        public int ResolutionY { get; }
        public int ResolutionZ { get; }
        public int ControlPointCount => ResolutionX * ResolutionY * ResolutionZ;
        public ReadOnlySpan<Vector3> ControlPoints => _controlPoints;
        internal Vector3[] ControlPointBuffer => _controlPoints;

        #endregion

        #region Public Logic

        public int GetFlatIndex(int indexX, int indexY, int indexZ)
        {
            ValidateIndices(indexX, indexY, indexZ);

            return indexX * ResolutionY * ResolutionZ + indexY * ResolutionZ + indexZ;
        }

        public Vector3 GetControlPoint(int indexX, int indexY, int indexZ)
        {
            return _controlPoints[GetFlatIndex(indexX, indexY, indexZ)];
        }

        public Vector3 GetRestControlPoint(int indexX, int indexY, int indexZ)
        {
            return _restControlPoints[GetFlatIndex(indexX, indexY, indexZ)];
        }

        public void SetControlPoint(int indexX, int indexY, int indexZ, Vector3 position)
        {
            _controlPoints[GetFlatIndex(indexX, indexY, indexZ)] = position;
        }

        public bool HasDeformation(float tolerance = MathConstants.LengthTolerance)
        {
            var toleranceSquared = tolerance * tolerance;

            for (var index = 0; index < _controlPoints.Length; index++)
            {
                if ((_controlPoints[index] - _restControlPoints[index]).LengthSquared > toleranceSquared)
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            _restControlPoints.CopyTo(_controlPoints, 0);
        }

        #endregion

        #region Private Logic

        private void InitializeControlPoints()
        {
            var size = Bounds.Size;

            for (var indexX = 0; indexX < ResolutionX; indexX++)
            {
                var s = indexX / (float)(ResolutionX - 1);

                for (var indexY = 0; indexY < ResolutionY; indexY++)
                {
                    var t = indexY / (float)(ResolutionY - 1);

                    for (var indexZ = 0; indexZ < ResolutionZ; indexZ++)
                    {
                        var u = indexZ / (float)(ResolutionZ - 1);
                        var flatIndex = GetFlatIndex(indexX, indexY, indexZ);

                        var position = new Vector3(
                            Bounds.Min.X + s * size.X,
                            Bounds.Min.Y + t * size.Y,
                            Bounds.Min.Z + u * size.Z);

                        _restControlPoints[flatIndex] = position;
                        _controlPoints[flatIndex] = position;
                    }
                }
            }
        }

        private void ValidateIndices(int indexX, int indexY, int indexZ)
        {
            if (indexX < 0 || indexX >= ResolutionX ||
                indexY < 0 || indexY >= ResolutionY ||
                indexZ < 0 || indexZ >= ResolutionZ)
            {
                throw new ArgumentOutOfRangeException(nameof(indexX), "FFD lattice indices are outside the lattice resolution.");
            }
        }

        private static AxisAlignedBoundingBox CreateUsableBounds(AxisAlignedBoundingBox sourceBounds)
        {
            var min = sourceBounds.Min;
            var max = sourceBounds.Max;
            var size = sourceBounds.Size;
            var largestDimension = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
            var padding = MathF.Max(1f, largestDimension) * 0.025f;

            ExpandDegenerateAxis(ref min.X, ref max.X, padding);
            ExpandDegenerateAxis(ref min.Y, ref max.Y, padding);
            ExpandDegenerateAxis(ref min.Z, ref max.Z, padding);

            return new AxisAlignedBoundingBox(min, max);
        }

        private static void ExpandDegenerateAxis(ref float min, ref float max, float padding)
        {
            if (max - min >= MathConstants.LengthTolerance)
            {
                return;
            }

            var center = (min + max) * 0.5f;
            min = center - padding;
            max = center + padding;
        }

        #endregion
    }
}
