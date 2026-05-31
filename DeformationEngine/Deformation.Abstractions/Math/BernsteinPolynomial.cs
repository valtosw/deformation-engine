namespace Deformation.Abstractions.Math
{
    public static class BernsteinPolynomial
    {
        #region Public Logic

        public static void FillBasisAndDerivative(int degree, float parameter, Span<float> basis, Span<float> derivative)
        {
            FillBasisOnly(degree, parameter, basis);
            derivative.Clear();

            if (degree == 0)
            {
                return;
            }

            Span<float> lowerDegreeBasis = stackalloc float[degree];
            FillBasisOnly(degree - 1, parameter, lowerDegreeBasis);

            for (var index = 0; index <= degree; index++)
            {
                var previous = index > 0 ? lowerDegreeBasis[index - 1] : 0f;
                var current = index < degree ? lowerDegreeBasis[index] : 0f;

                derivative[index] = degree * (previous - current);
            }
        }

        public static void FillBasisOnly(int degree, float parameter, Span<float> basis)
        {
            basis.Clear();
            basis[0] = 1f;

            var parameterValue = System.Math.Clamp(parameter, 0f, 1f);
            var inverseParameterValue = 1f - parameterValue;

            for (var currentDegree = 1; currentDegree <= degree; currentDegree++)
            {
                var saved = 0f;

                for (var index = 0; index < currentDegree; index++)
                {
                    var temporary = basis[index];
                    basis[index] = saved + inverseParameterValue * temporary;
                    saved = parameterValue * temporary;
                }

                basis[currentDegree] = saved;
            }
        }

        #endregion
    }
}