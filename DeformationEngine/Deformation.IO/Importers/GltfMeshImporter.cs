using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Abstractions.Skinning;
using Deformation.IO.Abstractions;
using Deformation.IO.Constants;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using OpenTkVector2 = OpenTK.Mathematics.Vector2;
using OpenTkVector3 = OpenTK.Mathematics.Vector3;
using OpenTkMatrix4 = OpenTK.Mathematics.Matrix4;
using EngineMesh = Deformation.Abstractions.Geometry.Mesh;
using SchemaMesh = SharpGLTF.Schema2.Mesh;
using SkinBone = Deformation.Abstractions.Skinning.Bone;
using VertexWeight = Deformation.Abstractions.Skinning.VertexWeight;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;
using NumericsVector4 = System.Numerics.Vector4;

namespace Deformation.IO.Importers
{
    public sealed class GltfMeshImporter : IMeshImporter
    {
        private static readonly byte[] FallbackPng =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
            0x54, 0x78, 0x9C, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
            0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xDD, 0x8D,
            0xB0, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
            0x44, 0xAE, 0x42, 0x60, 0x82
        ];

        public string[] SupportedExtensions =>
        [
            ImporterConstants.Gltf.TextExtension,
            ImporterConstants.Gltf.BinaryExtension
        ];

        public EngineMesh Load(string filePath)
        {
            var model = LoadModel(filePath);
            var vertices = new List<Vertex>();
            var indices = new List<uint>();
            var weightsByVertex = new List<List<VertexWeight>>();

            var skinContext = CreateSkinContext(model);
            var roots = model.DefaultScene?.VisualChildren ?? model.LogicalNodes.Where(node => node.VisualParent is null);

            foreach (var root in roots)
            {
                AddNodeMeshes(root, skinContext, vertices, indices, weightsByVertex);
            }

            var mesh = new EngineMesh([.. vertices], [.. indices]);

            if (skinContext.HasSkinning)
            {
                mesh.Skinning = new SkinningData(
                    CreateSkeleton(skinContext),
                    [.. weightsByVertex.Select(NormalizeAndLimitWeights)]);
            }

            return mesh;
        }

        private static ModelRoot LoadModel(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            var context = ReadContext.Create(resourceName =>
            {
                var normalizedResourceName = resourceName
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                var fullPath = Path.IsPathRooted(normalizedResourceName)
                    ? normalizedResourceName
                    : Path.Combine(directory, normalizedResourceName);

                if (File.Exists(fullPath))
                {
                    return File.ReadAllBytes(fullPath);
                }

                if (IsImageResource(resourceName))
                {
                    return FallbackPng;
                }

                throw new FileNotFoundException($"Could not find glTF resource '{resourceName}'.", fullPath);
            });

            context.ImageDecoder = _ => false;
            context.Validation = ValidationMode.Skip;

            return context.ReadSchema2(Path.GetFileName(filePath));
        }

        private static bool IsImageResource(string resourceName)
        {
            var extension = Path.GetExtension(resourceName);

            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".ktx2", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".dds", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddNodeMeshes(
            Node node,
            SkinContext skinContext,
            List<Vertex> vertices,
            List<uint> indices,
            List<List<VertexWeight>> weightsByVertex)
        {
            if (node.Mesh is not null)
            {
                AddMesh(node, node.Mesh, skinContext, vertices, indices, weightsByVertex);
            }

            foreach (var child in node.VisualChildren)
            {
                AddNodeMeshes(child, skinContext, vertices, indices, weightsByVertex);
            }
        }

        private static void AddMesh(
            Node node,
            SchemaMesh schemaMesh,
            SkinContext skinContext,
            List<Vertex> vertices,
            List<uint> indices,
            List<List<VertexWeight>> weightsByVertex)
        {
            var isSkinned = node.Skin is not null && skinContext.HasSkinning;
            var worldTransform = ToOpenTkMatrix(node.WorldMatrix);
            var normalTransform = worldTransform;
            normalTransform.Invert();
            normalTransform.Transpose();

            foreach (var primitive in schemaMesh.Primitives)
            {
                if (primitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
                {
                    continue;
                }

                var vertexOffset = vertices.Count;
                var positions = primitive.GetVertexAccessor("POSITION")?.AsVector3Array();

                if (positions is null)
                {
                    continue;
                }

                var normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var texCoords = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                var joints = primitive.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
                var weights = primitive.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

                for (var index = 0; index < positions.Count; index++)
                {
                    var position = ToOpenTkVector3(positions[index]);
                    var normal = normals is not null ? ToOpenTkVector3(normals[index]) : OpenTkVector3.Zero;

                    if (!isSkinned)
                    {
                        position = worldTransform.TransformPoint(position);

                        if (normal.LengthSquared > MathConstants.LengthTolerance)
                        {
                            normal = normalTransform.TransformDirection(normal).Normalized();
                        }
                    }

                    var texCoord = texCoords is not null ? ToOpenTkVector2(texCoords[index]) : OpenTkVector2.Zero;

                    vertices.Add(new Vertex(position, normal, texCoord));
                    weightsByVertex.Add([]);

                    if (isSkinned && joints is not null && weights is not null)
                    {
                        AddVertexWeights(weightsByVertex[^1], node.Skin!, skinContext, joints[index], weights[index]);
                    }
                }

                foreach (var (a, b, c) in primitive.GetTriangleIndices())
                {
                    indices.Add((uint)(vertexOffset + a));
                    indices.Add((uint)(vertexOffset + b));
                    indices.Add((uint)(vertexOffset + c));
                }
            }
        }

        private static SkinContext CreateSkinContext(ModelRoot model)
        {
            var context = new SkinContext();

            foreach (var skin in model.LogicalSkins)
            {
                for (var jointIndex = 0; jointIndex < skin.JointsCount; jointIndex++)
                {
                    var (jointNode, inverseBindMatrix) = skin.GetJoint(jointIndex);

                    if (!context.BoneIndexByNodeIndex.TryGetValue(jointNode.LogicalIndex, out var boneIndex))
                    {
                        boneIndex = context.Bones.Count;
                        context.BoneIndexByNodeIndex.Add(jointNode.LogicalIndex, boneIndex);
                        context.Bones.Add(new PendingBone(jointNode, ToOpenTkMatrix(inverseBindMatrix)));
                    }

                    context.BoneIndexBySkinJoint[(skin.LogicalIndex, jointIndex)] = boneIndex;
                }
            }

            return context;
        }

        private static Skeleton CreateSkeleton(SkinContext context)
        {
            var bones = new SkinBone[context.Bones.Count];

            for (var boneIndex = 0; boneIndex < context.Bones.Count; boneIndex++)
            {
                var pendingBone = context.Bones[boneIndex];
                var parentIndex = FindNearestBoneParentIndex(pendingBone.Node, context.BoneIndexByNodeIndex);

                bones[boneIndex] = new SkinBone(
                    boneIndex,
                    string.IsNullOrWhiteSpace(pendingBone.Node.Name) ? $"Joint {pendingBone.Node.LogicalIndex}" : pendingBone.Node.Name,
                    parentIndex,
                    GetLocalTransformToBoneParent(pendingBone.Node, context.BoneIndexByNodeIndex),
                    pendingBone.InverseBindTransform);
            }

            foreach (var bone in bones)
            {
                if (bone.ParentIndex is int parentIndex)
                {
                    bones[parentIndex].Children.Add(bone.Index);
                }
            }

            return new Skeleton(bones);
        }

        private static int? FindNearestBoneParentIndex(Node node, Dictionary<int, int> boneIndexByNodeIndex)
        {
            var parent = node.VisualParent;

            while (parent is not null)
            {
                if (boneIndexByNodeIndex.TryGetValue(parent.LogicalIndex, out var parentIndex))
                {
                    return parentIndex;
                }

                parent = parent.VisualParent;
            }

            return null;
        }

        private static OpenTkMatrix4 GetLocalTransformToBoneParent(Node node, Dictionary<int, int> boneIndexByNodeIndex)
        {
            var transform = ToOpenTkMatrix(node.LocalMatrix);
            var parent = node.VisualParent;

            while (parent is not null && !boneIndexByNodeIndex.ContainsKey(parent.LogicalIndex))
            {
                transform = transform * ToOpenTkMatrix(parent.LocalMatrix);
                parent = parent.VisualParent;
            }

            return transform;
        }

        private static void AddVertexWeights(
            List<VertexWeight> target,
            Skin skin,
            SkinContext context,
            NumericsVector4 joints,
            NumericsVector4 weights)
        {
            AddVertexWeight(target, skin, context, (int)MathF.Round(joints.X), weights.X);
            AddVertexWeight(target, skin, context, (int)MathF.Round(joints.Y), weights.Y);
            AddVertexWeight(target, skin, context, (int)MathF.Round(joints.Z), weights.Z);
            AddVertexWeight(target, skin, context, (int)MathF.Round(joints.W), weights.W);
        }

        private static void AddVertexWeight(List<VertexWeight> target, Skin skin, SkinContext context, int skinJointIndex, float weight)
        {
            if (weight <= MathConstants.ZeroTolerance)
            {
                return;
            }

            if (context.BoneIndexBySkinJoint.TryGetValue((skin.LogicalIndex, skinJointIndex), out var boneIndex))
            {
                target.Add(new VertexWeight(boneIndex, weight));
            }
        }

        private static VertexWeight[] NormalizeAndLimitWeights(List<VertexWeight> weights)
        {
            if (weights.Count == 0)
            {
                return [];
            }

            var limitedWeights = weights
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

        private static OpenTkVector2 ToOpenTkVector2(NumericsVector2 value)
        {
            return new OpenTkVector2(value.X, value.Y);
        }

        private static OpenTkVector3 ToOpenTkVector3(NumericsVector3 value)
        {
            return new OpenTkVector3(value.X, value.Y, value.Z);
        }

        private static OpenTkMatrix4 ToOpenTkMatrix(NumericsMatrix4x4 value)
        {
            return new OpenTkMatrix4(
                value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                value.M41, value.M42, value.M43, value.M44);
        }

        private sealed class SkinContext
        {
            public List<PendingBone> Bones { get; } = [];
            public Dictionary<int, int> BoneIndexByNodeIndex { get; } = [];
            public Dictionary<(int SkinIndex, int JointIndex), int> BoneIndexBySkinJoint { get; } = [];
            public bool HasSkinning => Bones.Count > 0;
        }

        private sealed class PendingBone(Node node, OpenTkMatrix4 inverseBindTransform)
        {
            public Node Node { get; } = node;
            public OpenTkMatrix4 InverseBindTransform { get; } = inverseBindTransform;
        }
    }
}
