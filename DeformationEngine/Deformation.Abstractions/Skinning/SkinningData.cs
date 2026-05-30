namespace Deformation.Abstractions.Skinning
{
    public sealed class SkinningData
    {
        public SkinningData(Skeleton skeleton, VertexWeight[][] vertexWeights)
        {
            Skeleton = skeleton;
            VertexWeights = vertexWeights;
        }

        public Skeleton Skeleton { get; }
        public VertexWeight[][] VertexWeights { get; }
        public bool CanSkin => Skeleton.HasBones && VertexWeights.Length > 0;
    }
}
