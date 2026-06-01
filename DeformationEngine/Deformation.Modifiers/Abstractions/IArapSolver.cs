using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Deformers.Arap;

namespace Deformation.Modifiers.Abstractions
{
    public interface IArapSolver
    {
        bool IsUnavailable { get; }

        void Clear();
        void Invalidate();
        bool TryPrepare(ArapSolverContext context);
        void Solve(ArapSolverContext context);
        void ApplyDeformation(Mesh mesh, ArapSolverContext context);
    }
}