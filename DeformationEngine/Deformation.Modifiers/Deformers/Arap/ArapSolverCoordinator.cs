using Deformation.Modifiers.Abstractions;

namespace Deformation.Modifiers.Deformers.Arap
{
    internal sealed class ArapSolverCoordinator
    {
        #region Fields

        private const int ExactSolveVertexLimit = 75000;
        private const int ExactSolveUnknownLimit = 60000;

        private readonly ArapExactSolver _exactSolver = new();
        private readonly ArapPreviewSolver _previewSolver = new();

        #endregion

        #region Public Logic

        public void Initialize(int vertexCount)
        {
            _exactSolver.Initialize(vertexCount);
            _previewSolver.Clear();
        }

        public void Clear()
        {
            _exactSolver.Clear();
            _previewSolver.Clear();
        }

        public void Invalidate()
        {
            _exactSolver.Invalidate();
            _previewSolver.Invalidate();
        }

        public IArapSolver SelectSolver(ArapSolverContext context)
        {
            if (ShouldUsePreviewSolve(context))
            {
                return _previewSolver;
            }

            if (!_exactSolver.TryPrepare(context))
            {
                return _previewSolver;
            }

            return _exactSolver;
        }

        #endregion

        #region Private Logic

        private bool ShouldUsePreviewSolve(ArapSolverContext context)
        {
            if (_exactSolver.IsUnavailable || context.OriginalPositions.Length > ExactSolveVertexLimit)
            {
                return true;
            }

            var unknownCount = 0;

            for (var index = 0; index < context.Constrained.Length; index++)
            {
                if (!context.Constrained[index] && context.Neighbors[index].Length > 0)
                {
                    unknownCount++;
                }
            }

            return unknownCount > ExactSolveUnknownLimit;
        }

        #endregion
    }
}