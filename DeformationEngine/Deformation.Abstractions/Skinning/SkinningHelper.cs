using Deformation.Abstractions.Constants;

namespace Deformation.Abstractions.Skinning
{
    public static class SkinningHelper
    {
        #region Public Logic

        public static VertexWeight[] NormalizeAndLimitWeights(IEnumerable<VertexWeight> weights)
        {
            var weightList = weights as IReadOnlyList<VertexWeight> ?? [.. weights];

            if (weightList.Count == 0)
            {
                return [];
            }

            var limitedWeights = weightList
                .GroupBy(weight => weight.BoneIndex)
                .Select(group => new VertexWeight(group.Key, group.Sum(weight => weight.Weight)))
                .OrderByDescending(weight => weight.Weight)
                .Take(4)
                .ToArray();

            var totalWeight = limitedWeights.Sum(weight => weight.Weight);

            if (totalWeight <= MathConstants.ZeroTolerance)
            {
                return [];
            }

            for (var index = 0; index < limitedWeights.Length; index++)
            {
                limitedWeights[index] = new VertexWeight(limitedWeights[index].BoneIndex, limitedWeights[index].Weight / totalWeight);
            }

            return limitedWeights;
        }

        #endregion
    }
}