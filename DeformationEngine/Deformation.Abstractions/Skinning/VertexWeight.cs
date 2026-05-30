namespace Deformation.Abstractions.Skinning
{
    public readonly struct VertexWeight(int boneIndex, float weight)
    {
        public int BoneIndex { get; } = boneIndex;
        public float Weight { get; } = weight;
    }
}
