using OpenTK.Mathematics;

namespace Deformation.Modifiers.Deformers.Arap
{
    public sealed class ArapSolverContext
    {
        #region Properties

        public Vector3[] OriginalPositions { get; init; } = [];
        public Vector3[] WorkingPositions { get; init; } = [];
        public Vector3[] ConstraintPositions { get; init; } = [];
        public int[][] Neighbors { get; init; } = [];
        public bool[] Constrained { get; init; } = [];
        public int ConstraintVersion { get; init; }
        public int Iterations { get; init; }
        public bool UseIdentityRotations { get; init; }
        public IReadOnlySet<int> ControlVertices { get; init; } = new HashSet<int>();
        public Func<Vector3, Vector3> TransformControlPoint { get; init; } = _ => Vector3.Zero;

        #endregion
    }
}