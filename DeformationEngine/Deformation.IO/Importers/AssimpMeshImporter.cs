using Assimp;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Abstractions.Skinning;
using Deformation.IO.Abstractions;
using Deformation.IO.Constants;
using OpenTK.Mathematics;
using SkinBone = Deformation.Abstractions.Skinning.Bone;
using Mesh = Deformation.Abstractions.Geometry.Mesh;
using VertexWeight = Deformation.Abstractions.Skinning.VertexWeight;

namespace Deformation.IO.Importers
{
    public sealed class AssimpMeshImporter : IMeshImporter
    {
        public string[] SupportedExtensions =>
        [
            ImporterConstants.Collada.Extension
        ];

        public Mesh Load(string filePath)
        {
            using var context = new AssimpContext();
            var scene = context.ImportFile(filePath, PostProcessSteps.Triangulate | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.JoinIdenticalVertices);

            if (scene is null || !scene.HasMeshes)
            {
                throw new InvalidDataException("The model does not contain any mesh geometry.");
            }

            var vertices = new List<Vertex>();
            var indices = new List<uint>();
            var weightsByVertex = new List<List<VertexWeight>>();
            var boneNames = CollectBoneNames(scene);

            if (boneNames.Count == 0)
            {
                return LoadStaticScene(scene);
            }

            var boneIndexByName = boneNames
                .Select((name, index) => new { name, index })
                .ToDictionary(item => item.name, item => item.index, StringComparer.Ordinal);
            var inverseBindTransforms = CreateInverseBindTransforms(scene, boneIndexByName);

            foreach (var assimpMesh in scene.Meshes)
            {
                var vertexOffset = vertices.Count;

                for (var index = 0; index < assimpMesh.VertexCount; index++)
                {
                    var position = ToVector3(assimpMesh.Vertices[index]);
                    var normal = assimpMesh.HasNormals ? ToVector3(assimpMesh.Normals[index]) : Vector3.Zero;
                    var texCoords = assimpMesh.HasTextureCoords(0)
                        ? new Vector2(assimpMesh.TextureCoordinateChannels[0][index].X, assimpMesh.TextureCoordinateChannels[0][index].Y)
                        : Vector2.Zero;

                    vertices.Add(new Vertex(position, normal, texCoords));
                    weightsByVertex.Add([]);
                }

                foreach (var face in assimpMesh.Faces.Where(face => face.IndexCount == 3))
                {
                    indices.Add((uint)(vertexOffset + face.Indices[0]));
                    indices.Add((uint)(vertexOffset + face.Indices[1]));
                    indices.Add((uint)(vertexOffset + face.Indices[2]));
                }

                foreach (var bone in assimpMesh.Bones)
                {
                    if (!boneIndexByName.TryGetValue(bone.Name, out var boneIndex))
                    {
                        continue;
                    }

                    foreach (var weight in bone.VertexWeights)
                    {
                        var targetVertex = vertexOffset + weight.VertexID;

                        if (targetVertex >= 0 && targetVertex < weightsByVertex.Count && weight.Weight > 0f)
                        {
                            weightsByVertex[targetVertex].Add(new VertexWeight(boneIndex, weight.Weight));
                        }
                    }
                }
            }

            var mesh = new Mesh([.. vertices], [.. indices]);

            if (boneNames.Count > 0)
            {
                mesh.Skinning = new SkinningData(
                    CreateSkeleton(scene, boneNames, boneIndexByName, inverseBindTransforms),
                    [.. weightsByVertex.Select(NormalizeAndLimitWeights)]);
            }

            return mesh;
        }

        private static Mesh LoadStaticScene(Scene scene)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            AddStaticNodeMeshes(scene.RootNode, Matrix4.Identity, scene, vertices, indices);

            return new Mesh([.. vertices], [.. indices]);
        }

        private static void AddStaticNodeMeshes(
            Node node,
            Matrix4 parentTransform,
            Scene scene,
            List<Vertex> vertices,
            List<uint> indices)
        {
            var worldTransform = ToMatrix4(node.Transform) * parentTransform;
            var normalTransform = worldTransform;
            normalTransform.Invert();
            normalTransform.Transpose();

            foreach (var meshIndex in node.MeshIndices)
            {
                var assimpMesh = scene.Meshes[meshIndex];
                var vertexOffset = vertices.Count;

                for (var index = 0; index < assimpMesh.VertexCount; index++)
                {
                    var position = worldTransform.TransformPoint(ToVector3(assimpMesh.Vertices[index]));
                    var normal = assimpMesh.HasNormals ? ToVector3(assimpMesh.Normals[index]) : Vector3.Zero;

                    if (normal.LengthSquared > MathConstants.LengthTolerance)
                    {
                        normal = normalTransform.TransformDirection(normal).Normalized();
                    }

                    var texCoords = assimpMesh.HasTextureCoords(0)
                        ? new Vector2(assimpMesh.TextureCoordinateChannels[0][index].X, assimpMesh.TextureCoordinateChannels[0][index].Y)
                        : Vector2.Zero;

                    vertices.Add(new Vertex(position, normal, texCoords));
                }

                foreach (var face in assimpMesh.Faces.Where(face => face.IndexCount == 3))
                {
                    indices.Add((uint)(vertexOffset + face.Indices[0]));
                    indices.Add((uint)(vertexOffset + face.Indices[1]));
                    indices.Add((uint)(vertexOffset + face.Indices[2]));
                }
            }

            foreach (var child in node.Children)
            {
                AddStaticNodeMeshes(child, worldTransform, scene, vertices, indices);
            }
        }

        private static List<string> CollectBoneNames(Scene scene)
        {
            return [.. scene.Meshes
                .SelectMany(mesh => mesh.Bones)
                .Select(bone => bone.Name)
                .Distinct(StringComparer.Ordinal)];
        }

        private static Dictionary<int, Matrix4> CreateInverseBindTransforms(Scene scene, Dictionary<string, int> boneIndexByName)
        {
            var inverseBindTransforms = new Dictionary<int, Matrix4>();

            foreach (var bone in scene.Meshes.SelectMany(mesh => mesh.Bones))
            {
                if (boneIndexByName.TryGetValue(bone.Name, out var boneIndex))
                {
                    inverseBindTransforms[boneIndex] = ToMatrix4(bone.OffsetMatrix);
                }
            }

            return inverseBindTransforms;
        }

        private static Skeleton CreateSkeleton(
            Scene scene,
            IReadOnlyList<string> boneNames,
            Dictionary<string, int> boneIndexByName,
            Dictionary<int, Matrix4> inverseBindTransforms)
        {
            var nodeByName = new Dictionary<string, Node>(StringComparer.Ordinal);
            RegisterNodes(scene.RootNode, nodeByName);

            var bones = new SkinBone[boneNames.Count];

            for (var boneIndex = 0; boneIndex < boneNames.Count; boneIndex++)
            {
                var boneName = boneNames[boneIndex];
                nodeByName.TryGetValue(boneName, out var node);

                var parentIndex = FindNearestBoneParentIndex(node, boneIndexByName);

                var ibt = inverseBindTransforms.TryGetValue(boneIndex, out var inv) ? inv : Matrix4.Identity;
                var bindWorld = ibt.Inverted();
                var localTransform = bindWorld;

                if (parentIndex is int pIndex)
                {
                    var parentIbt = inverseBindTransforms.TryGetValue(pIndex, out var pInv) ? pInv : Matrix4.Identity;
                    var parentBindWorld = parentIbt.Inverted();
                    localTransform = bindWorld * parentBindWorld.Inverted();
                }

                bones[boneIndex] = new SkinBone(
                    boneIndex,
                    boneName,
                    parentIndex,
                    localTransform,
                    ibt);
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

        private static void RegisterNodes(Node node, Dictionary<string, Node> nodeByName)
        {
            if (!string.IsNullOrWhiteSpace(node.Name))
            {
                nodeByName[node.Name] = node;
            }

            foreach (var child in node.Children)
            {
                RegisterNodes(child, nodeByName);
            }
        }

        private static int? FindNearestBoneParentIndex(Node? node, Dictionary<string, int> boneIndexByName)
        {
            var parent = node?.Parent;

            while (parent is not null)
            {
                if (boneIndexByName.TryGetValue(parent.Name, out var parentIndex))
                {
                    return parentIndex;
                }

                parent = parent.Parent;
            }

            return null;
        }

        private static VertexWeight[] NormalizeAndLimitWeights(List<VertexWeight> weights)
        {
            if (weights.Count == 0)
            {
                return [];
            }

            var limitedWeights = weights
                .OrderByDescending(weight => weight.Weight)
                .Take(4)
                .ToArray();

            var totalWeight = limitedWeights.Sum(weight => weight.Weight);

            if (totalWeight <= 0f)
            {
                return [];
            }

            for (var index = 0; index < limitedWeights.Length; index++)
            {
                limitedWeights[index] = new VertexWeight(limitedWeights[index].BoneIndex, limitedWeights[index].Weight / totalWeight);
            }

            return limitedWeights;
        }

        private static Vector3 ToVector3(Vector3D value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static Matrix4 ToMatrix4(Matrix4x4 value)
        {
            return new Matrix4(
                value.A1, value.A2, value.A3, value.A4,
                value.B1, value.B2, value.B3, value.B4,
                value.C1, value.C2, value.C3, value.C4,
                value.D1, value.D2, value.D3, value.D4);
        }
    }
}