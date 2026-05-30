using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;

namespace Deformation.Modifiers.Deformers
{
    public sealed class LbsDeformer : IDeformer
    {
        public bool IsEnabled { get; set; }

        public void Deform(Mesh mesh)
        {
            var skinning = mesh.Skinning;

            if (!IsEnabled || skinning?.CanSkin != true || skinning.VertexWeights.Length != mesh.Vertices.Length)
            {
                return;
            }

            var skeleton = skinning.Skeleton;
            skeleton.UpdateWorldTransforms();

            for (var vertexIndex = 0; vertexIndex < mesh.Vertices.Length; vertexIndex++)
            {
                var influences = skinning.VertexWeights[vertexIndex];

                if (influences.Length == 0)
                {
                    continue;
                }

                var vertex = mesh.Vertices[vertexIndex];
                var skinnedPosition = OpenTK.Mathematics.Vector3.Zero;
                var skinnedNormal = OpenTK.Mathematics.Vector3.Zero;
                var totalWeight = 0f;

                foreach (var influence in influences)
                {
                    if (influence.BoneIndex < 0 || influence.BoneIndex >= skeleton.Bones.Count)
                    {
                        continue;
                    }

                    var bone = skeleton.Bones[influence.BoneIndex];
                    var skinMatrix = bone.InverseBindTransform * bone.WorldTransform;
                    var weight = influence.Weight;

                    skinnedPosition += skinMatrix.TransformPoint(vertex.Position) * weight;

                    if (vertex.Normal.LengthSquared > MathConstants.LengthTolerance)
                    {
                        skinnedNormal += skinMatrix.TransformDirection(vertex.Normal) * weight;
                    }

                    totalWeight += weight;
                }

                if (totalWeight <= MathConstants.ZeroTolerance)
                {
                    continue;
                }

                var inverseWeight = 1f / totalWeight;
                skinnedPosition *= inverseWeight;

                if (skinnedNormal.LengthSquared > MathConstants.LengthTolerance)
                {
                    skinnedNormal = (skinnedNormal * inverseWeight).Normalized();
                }
                else
                {
                    skinnedNormal = vertex.Normal;
                }

                mesh.Vertices[vertexIndex] = new Vertex(skinnedPosition, skinnedNormal, vertex.TexCoords);
            }
        }
    }
}
