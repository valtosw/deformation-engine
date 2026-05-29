using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers
{
    public sealed class FfdDeformer : IDeformer
    {
        #region Constants

        public const int MinimumResolution = 2;
        public const int MaximumResolution = 8;

        private const long MaximumPrecomputedBasisFloats = 64_000_000;

        #endregion

        #region Fields

        private FfdLattice? _lattice;
        private Vector3[]? _vertexParameters;

        private float[]? _basisX;
        private float[]? _basisY;
        private float[]? _basisZ;
        private float[]? _derivativeX;
        private float[]? _derivativeY;
        private float[]? _derivativeZ;

        private int _resolutionX;
        private int _resolutionY;
        private int _resolutionZ;

        #endregion

        #region Properties

        public FfdLattice? Lattice => _lattice;
        public bool IsInitialized => _lattice is not null && _vertexParameters is not null;
        public bool HasChanges => _lattice?.HasDeformation() == true;

        #endregion

        #region Public Logic

        public void Initialize(Mesh originalMesh, int resolutionX, int resolutionY, int resolutionZ)
        {
            _resolutionX = ClampResolution(resolutionX);
            _resolutionY = ClampResolution(resolutionY);
            _resolutionZ = ClampResolution(resolutionZ);

            _lattice = new FfdLattice(originalMesh.LocalBoundingBox, _resolutionX, _resolutionY, _resolutionZ);
            _vertexParameters = new Vector3[originalMesh.Vertices.Length];

            var totalBasisFloats = (long)originalMesh.Vertices.Length * (_resolutionX + _resolutionY + _resolutionZ) * 2L;
            var shouldPrecomputeBasis = totalBasisFloats <= MaximumPrecomputedBasisFloats;

            if (shouldPrecomputeBasis)
            {
                _basisX = new float[originalMesh.Vertices.Length * _resolutionX];
                _basisY = new float[originalMesh.Vertices.Length * _resolutionY];
                _basisZ = new float[originalMesh.Vertices.Length * _resolutionZ];
                _derivativeX = new float[originalMesh.Vertices.Length * _resolutionX];
                _derivativeY = new float[originalMesh.Vertices.Length * _resolutionY];
                _derivativeZ = new float[originalMesh.Vertices.Length * _resolutionZ];

                PrecomputeParametersAndBasis(originalMesh);
            }
            else
            {
                _basisX = null;
                _basisY = null;
                _basisZ = null;
                _derivativeX = null;
                _derivativeY = null;
                _derivativeZ = null;

                PrecomputeParametersOnly(originalMesh);
            }
        }

        public void Clear()
        {
            _lattice = null;
            _vertexParameters = null;
            _basisX = null;
            _basisY = null;
            _basisZ = null;
            _derivativeX = null;
            _derivativeY = null;
            _derivativeZ = null;
            _resolutionX = 0;
            _resolutionY = 0;
            _resolutionZ = 0;
        }

        public void Reset()
        {
            _lattice?.Reset();
        }

        public void UpdateControlPoint(int indexX, int indexY, int indexZ, Vector3 position)
        {
            _lattice?.SetControlPoint(indexX, indexY, indexZ, position);
        }

        public Vector3 GetControlPoint(int indexX, int indexY, int indexZ)
        {
            return _lattice?.GetControlPoint(indexX, indexY, indexZ) ?? Vector3.Zero;
        }

        public void Deform(Mesh mesh)
        {
            if (_lattice is null ||
                _vertexParameters is null ||
                _vertexParameters.Length != mesh.Vertices.Length)
            {
                return;
            }

            if (!HasChanges)
            {
                return;
            }

            if (_basisX is not null &&
                _basisY is not null &&
                _basisZ is not null &&
                _derivativeX is not null &&
                _derivativeY is not null &&
                _derivativeZ is not null)
            {
                DeformWithPrecomputedBasis(mesh);
            }
            else
            {
                DeformWithTemporaryBasis(mesh);
            }
        }

        #endregion

        #region Private Logic

        private void PrecomputeParametersAndBasis(Mesh originalMesh)
        {
            if (_lattice is null ||
                _vertexParameters is null ||
                _basisX is null ||
                _basisY is null ||
                _basisZ is null ||
                _derivativeX is null ||
                _derivativeY is null ||
                _derivativeZ is null)
            {
                return;
            }

            var bounds = _lattice.Bounds;

            Parallel.For(0, originalMesh.Vertices.Length, index =>
            {
                var parameters = CalculateParameters(originalMesh.Vertices[index].Position, bounds);
                _vertexParameters[index] = parameters;

                FillBernsteinBasisAndDerivative(
                    _resolutionX - 1,
                    parameters.X,
                    _basisX.AsSpan(index * _resolutionX, _resolutionX),
                    _derivativeX.AsSpan(index * _resolutionX, _resolutionX));

                FillBernsteinBasisAndDerivative(
                    _resolutionY - 1,
                    parameters.Y,
                    _basisY.AsSpan(index * _resolutionY, _resolutionY),
                    _derivativeY.AsSpan(index * _resolutionY, _resolutionY));

                FillBernsteinBasisAndDerivative(
                    _resolutionZ - 1,
                    parameters.Z,
                    _basisZ.AsSpan(index * _resolutionZ, _resolutionZ),
                    _derivativeZ.AsSpan(index * _resolutionZ, _resolutionZ));
            });
        }

        private void PrecomputeParametersOnly(Mesh originalMesh)
        {
            if (_lattice is null || _vertexParameters is null)
            {
                return;
            }

            var bounds = _lattice.Bounds;

            Parallel.For(0, originalMesh.Vertices.Length, index =>
            {
                _vertexParameters[index] = CalculateParameters(originalMesh.Vertices[index].Position, bounds);
            });
        }

        private void DeformWithPrecomputedBasis(Mesh mesh)
        {
            if (_lattice is null ||
                _basisX is null ||
                _basisY is null ||
                _basisZ is null ||
                _derivativeX is null ||
                _derivativeY is null ||
                _derivativeZ is null)
            {
                return;
            }

            var controlPoints = _lattice.ControlPointBuffer;
            var latticeSize = _lattice.Bounds.Size;

            Parallel.For(0, mesh.Vertices.Length, index =>
            {
                mesh.Vertices[index] = DeformVertex(
                    mesh.Vertices[index],
                    controlPoints,
                    _resolutionX,
                    _resolutionY,
                    _resolutionZ,
                    _basisX.AsSpan(index * _resolutionX, _resolutionX),
                    _basisY.AsSpan(index * _resolutionY, _resolutionY),
                    _basisZ.AsSpan(index * _resolutionZ, _resolutionZ),
                    _derivativeX.AsSpan(index * _resolutionX, _resolutionX),
                    _derivativeY.AsSpan(index * _resolutionY, _resolutionY),
                    _derivativeZ.AsSpan(index * _resolutionZ, _resolutionZ),
                    latticeSize);
            });
        }

        private void DeformWithTemporaryBasis(Mesh mesh)
        {
            if (_lattice is null || _vertexParameters is null)
            {
                return;
            }

            var controlPoints = _lattice.ControlPointBuffer;
            var latticeSize = _lattice.Bounds.Size;

            Parallel.For(0, mesh.Vertices.Length, index =>
            {
                Span<float> basisX = stackalloc float[_resolutionX];
                Span<float> basisY = stackalloc float[_resolutionY];
                Span<float> basisZ = stackalloc float[_resolutionZ];
                Span<float> derivativeX = stackalloc float[_resolutionX];
                Span<float> derivativeY = stackalloc float[_resolutionY];
                Span<float> derivativeZ = stackalloc float[_resolutionZ];

                var parameters = _vertexParameters[index];

                FillBernsteinBasisAndDerivative(_resolutionX - 1, parameters.X, basisX, derivativeX);
                FillBernsteinBasisAndDerivative(_resolutionY - 1, parameters.Y, basisY, derivativeY);
                FillBernsteinBasisAndDerivative(_resolutionZ - 1, parameters.Z, basisZ, derivativeZ);

                mesh.Vertices[index] = DeformVertex(
                    mesh.Vertices[index],
                    controlPoints,
                    _resolutionX,
                    _resolutionY,
                    _resolutionZ,
                    basisX,
                    basisY,
                    basisZ,
                    derivativeX,
                    derivativeY,
                    derivativeZ,
                    latticeSize);
            });
        }

        private static Vertex DeformVertex(
            Vertex sourceVertex,
            Vector3[] controlPoints,
            int resolutionX,
            int resolutionY,
            int resolutionZ,
            ReadOnlySpan<float> basisX,
            ReadOnlySpan<float> basisY,
            ReadOnlySpan<float> basisZ,
            ReadOnlySpan<float> derivativeX,
            ReadOnlySpan<float> derivativeY,
            ReadOnlySpan<float> derivativeZ,
            Vector3 latticeSize)
        {
            var position = Vector3.Zero;
            var derivativeS = Vector3.Zero;
            var derivativeT = Vector3.Zero;
            var derivativeU = Vector3.Zero;

            for (var indexX = 0; indexX < resolutionX; indexX++)
            {
                var weightX = basisX[indexX];
                var derivativeWeightX = derivativeX[indexX];

                for (var indexY = 0; indexY < resolutionY; indexY++)
                {
                    var weightXY = weightX * basisY[indexY];
                    var derivativeWeightXYByS = derivativeWeightX * basisY[indexY];
                    var derivativeWeightXYByT = weightX * derivativeY[indexY];

                    var baseIndex = indexX * resolutionY * resolutionZ + indexY * resolutionZ;

                    for (var indexZ = 0; indexZ < resolutionZ; indexZ++)
                    {
                        var controlPoint = controlPoints[baseIndex + indexZ];
                        var weightZ = basisZ[indexZ];
                        var derivativeWeightZ = derivativeZ[indexZ];

                        position += controlPoint * (weightXY * weightZ);
                        derivativeS += controlPoint * (derivativeWeightXYByS * weightZ);
                        derivativeT += controlPoint * (derivativeWeightXYByT * weightZ);
                        derivativeU += controlPoint * (weightXY * derivativeWeightZ);
                    }
                }
            }

            var normal = TransformNormal(sourceVertex.Normal, derivativeS, derivativeT, derivativeU, latticeSize);

            return new Vertex(position, normal, sourceVertex.TexCoords);
        }

        private static Vector3 TransformNormal(Vector3 normal, Vector3 derivativeS, Vector3 derivativeT, Vector3 derivativeU, Vector3 latticeSize)
        {
            if (normal.LengthSquared < MathConstants.LengthTolerance)
            {
                return normal;
            }

            var normalizedNormal = normal.Normalized();
            var derivativeX = latticeSize.X > MathConstants.LengthTolerance ? derivativeS / latticeSize.X : Vector3.UnitX;
            var derivativeY = latticeSize.Y > MathConstants.LengthTolerance ? derivativeT / latticeSize.Y : Vector3.UnitY;
            var derivativeZ = latticeSize.Z > MathConstants.LengthTolerance ? derivativeU / latticeSize.Z : Vector3.UnitZ;

            var referenceAxis = MathF.Abs(Vector3.Dot(normalizedNormal, Vector3.UnitY)) < 0.95f
                ? Vector3.UnitY
                : Vector3.UnitX;

            var tangent = Vector3.Cross(referenceAxis, normalizedNormal);

            if (tangent.LengthSquared < MathConstants.LengthTolerance)
            {
                return normalizedNormal;
            }

            tangent.Normalize();

            var bitangent = Vector3.Cross(normalizedNormal, tangent);

            if (bitangent.LengthSquared < MathConstants.LengthTolerance)
            {
                return normalizedNormal;
            }

            bitangent.Normalize();

            var transformedTangent = TransformByGradient(tangent, derivativeX, derivativeY, derivativeZ);
            var transformedBitangent = TransformByGradient(bitangent, derivativeX, derivativeY, derivativeZ);
            var transformedNormal = Vector3.Cross(transformedTangent, transformedBitangent);

            return transformedNormal.LengthSquared > MathConstants.LengthTolerance
                ? transformedNormal.Normalized()
                : normalizedNormal;
        }

        private static Vector3 TransformByGradient(Vector3 direction, Vector3 derivativeX, Vector3 derivativeY, Vector3 derivativeZ)
        {
            return derivativeX * direction.X + derivativeY * direction.Y + derivativeZ * direction.Z;
        }

        private static Vector3 CalculateParameters(Vector3 position, AxisAlignedBoundingBox bounds)
        {
            var size = bounds.Size;

            return new Vector3(
                NormalizeParameter(position.X, bounds.Min.X, size.X),
                NormalizeParameter(position.Y, bounds.Min.Y, size.Y),
                NormalizeParameter(position.Z, bounds.Min.Z, size.Z));
        }

        private static float NormalizeParameter(float value, float min, float length)
        {
            if (length < MathConstants.LengthTolerance)
            {
                return 0.5f;
            }

            return Math.Clamp((value - min) / length, 0f, 1f);
        }

        private static void FillBernsteinBasisAndDerivative(int degree, float parameter, Span<float> basis, Span<float> derivative)
        {
            FillBernsteinBasisOnly(degree, parameter, basis);
            derivative.Clear();

            if (degree == 0)
            {
                return;
            }

            Span<float> lowerDegreeBasis = stackalloc float[degree];
            FillBernsteinBasisOnly(degree - 1, parameter, lowerDegreeBasis);

            for (var index = 0; index <= degree; index++)
            {
                var previous = index > 0 ? lowerDegreeBasis[index - 1] : 0f;
                var current = index < degree ? lowerDegreeBasis[index] : 0f;

                derivative[index] = degree * (previous - current);
            }
        }

        private static void FillBernsteinBasisOnly(int degree, float parameter, Span<float> basis)
        {
            basis.Clear();
            basis[0] = 1f;

            var t = Math.Clamp(parameter, 0f, 1f);
            var inverseT = 1f - t;

            for (var currentDegree = 1; currentDegree <= degree; currentDegree++)
            {
                var saved = 0f;

                for (var index = 0; index < currentDegree; index++)
                {
                    var temporary = basis[index];
                    basis[index] = saved + inverseT * temporary;
                    saved = t * temporary;
                }

                basis[currentDegree] = saved;
            }
        }

        private static int ClampResolution(int resolution)
        {
            return Math.Clamp(resolution, MinimumResolution, MaximumResolution);
        }

        #endregion
    }
}
