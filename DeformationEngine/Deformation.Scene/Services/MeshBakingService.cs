using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Abstractions.Skinning;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;

namespace Deformation.Scene.Services
{
    public sealed class MeshBakingService : IMeshBakingService
    {
        #region Public Logic

        public Mesh BakeMesh(MeshNode meshNode, TwistDeformer twistDeformer, BendDeformer bendDeformer, FfdDeformer ffdDeformer)
        {
            var currentDeformed = meshNode.DeformedMesh;

            if (currentDeformed is null)
            {
                return meshNode.Mesh ?? new Mesh([], []);
            }

            var worldMatrix = meshNode.LocalTransform;
            var normalMatrix = worldMatrix.GetNormalMatrix();

            var newVertices = new Vertex[currentDeformed.Vertices.Length];

            for (var index = 0; index < currentDeformed.Vertices.Length; index++)
            {
                var vertex = currentDeformed.Vertices[index];

                var transformedPosition = worldMatrix.TransformPoint(vertex.Position);
                var transformedNormal = vertex.Normal.LengthSquared > MathConstants.LengthTolerance
                    ? normalMatrix.TransformDirection(vertex.Normal).Normalized()
                    : vertex.Normal;

                newVertices[index] = new Vertex(transformedPosition, transformedNormal, vertex.TexCoords);
            }

            var newIndices = new uint[currentDeformed.Indices.Length];
            currentDeformed.Indices.CopyTo(newIndices, 0);

            var bakedMesh = new Mesh(newVertices, newIndices)
            {
                Topology = currentDeformed.Topology,
                Skinning = currentDeformed.Skinning
            };

            if (meshNode.Mesh?.Skinning is { } skinning)
            {
                BakeSkeleton(skinning.Skeleton, meshNode, worldMatrix, twistDeformer, bendDeformer, ffdDeformer);
            }

            return bakedMesh;
        }

        #endregion

        #region Private Logic

        private static void BakeSkeleton(
            Skeleton skeleton,
            MeshNode meshNode,
            Matrix4 worldMatrix,
            TwistDeformer twistDeformer,
            BendDeformer bendDeformer,
            FfdDeformer ffdDeformer)
        {
            skeleton.UpdateWorldTransforms();

            var boneVertices = new Vertex[skeleton.Bones.Count * 4];
            var epsilon = 0.01f;

            for (var index = 0; index < skeleton.Bones.Count; index++)
            {
                var bone = skeleton.Bones[index];
                var worldTransform = bone.WorldTransform;
                var position = worldTransform.ExtractTranslation();
                var xAxis = worldTransform.Row0.Xyz;
                var yAxis = worldTransform.Row1.Xyz;
                var zAxis = worldTransform.Row2.Xyz;

                boneVertices[index * 4 + 0] = new Vertex(position);
                boneVertices[index * 4 + 1] = new Vertex(position + xAxis * epsilon);
                boneVertices[index * 4 + 2] = new Vertex(position + yAxis * epsilon);
                boneVertices[index * 4 + 3] = new Vertex(position + zAxis * epsilon);
            }

            if (meshNode.Mesh is null)
            {
                return;
            }

            meshNode.Mesh.CalculateBounds(out var min, out var max);

            twistDeformer.Deform(boneVertices, min, max);
            bendDeformer.Deform(boneVertices, min, max);
            ffdDeformer.Deform(boneVertices);

            var newWorldTransforms = new Matrix4[skeleton.Bones.Count];

            for (var index = 0; index < skeleton.Bones.Count; index++)
            {
                var point0 = boneVertices[index * 4 + 0].Position;
                var point1 = boneVertices[index * 4 + 1].Position;
                var point2 = boneVertices[index * 4 + 2].Position;
                var point3 = boneVertices[index * 4 + 3].Position;

                var newPosition = worldMatrix.TransformPoint(point0);
                var newX = worldMatrix.TransformPoint(point1) - newPosition;
                var newY = worldMatrix.TransformPoint(point2) - newPosition;
                var newZ = worldMatrix.TransformPoint(point3) - newPosition;

                var scaleX = newX.Length / epsilon;
                var scaleY = newY.Length / epsilon;
                var scaleZ = newZ.Length / epsilon;

                newX.Normalize();
                newY = (newY - newX * Vector3.Dot(newY, newX)).Normalized();
                newZ = Vector3.Cross(newX, newY).Normalized();

                newX *= scaleX;
                newY *= scaleY;
                newZ *= scaleZ;

                var newWorldTransform = new Matrix4(
                    new Vector4(newX, 0f),
                    new Vector4(newY, 0f),
                    new Vector4(newZ, 0f),
                    new Vector4(newPosition, 1f)
                );

                newWorldTransforms[index] = newWorldTransform;
            }

            for (var index = 0; index < skeleton.Bones.Count; index++)
            {
                var bone = skeleton.Bones[index];

                if (bone.ParentIndex is int parentIndex)
                {
                    var parentWorldTransform = newWorldTransforms[parentIndex];
                    var inverseParentWorldTransform = parentWorldTransform.Inverted();
                    bone.LocalTransform = newWorldTransforms[index] * inverseParentWorldTransform;
                }
                else
                {
                    bone.LocalTransform = newWorldTransforms[index];
                }
            }
        }

        #endregion
    }
}