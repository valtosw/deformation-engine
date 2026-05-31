using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;

namespace Deformation.Scene.Builders
{
    public sealed class SkeletonVisualBuilder(IGizmoSystem gizmoSystem) : ISkeletonVisualBuilder
    {
        #region Fields

        private readonly List<BoneNode> _boneNodes = [];
        private MeshNode? _skeletonLineNode;
        private MeshNode? _activeParentNode;

        #endregion

        #region Properties

        public IReadOnlyList<BoneNode> BoneNodes
        {
            get
            {
                return _boneNodes;
            }
        }

        #endregion

        #region Public Logic

        public void Build(MeshNode parentNode, Mesh mesh, float targetSphereRadius, bool isVisible)
        {
            var skinning = mesh.Skinning;

            if (skinning is null)
            {
                return;
            }

            _activeParentNode = parentNode;
            var radius = MathF.Max(0.01f, targetSphereRadius * 0.02f);
            var jointMesh = MeshFactory.CreateSphere(radius, rings: 8, segments: 12, Vector3.Zero);
            gizmoSystem.BoneGizmoRadius = radius;

            var lineVertices = new List<Vertex>();
            var lineIndices = new List<uint>();

            var nodesByBoneIndex = new Dictionary<int, BoneNode>();

            foreach (var bone in skinning.Skeleton.Bones)
            {
                var boneNode = new BoneNode(bone, UpdateLines)
                {
                    Mesh = jointMesh,
                    Color = new Vector3(1f, 0.85f, 0.2f),
                    IsVisible = isVisible,
                    IgnoreDepth = false,
                    ForceSolid = true
                };

                nodesByBoneIndex.Add(bone.Index, boneNode);
                _boneNodes.Add(boneNode);
            }

            foreach (var boneNode in _boneNodes)
            {
                if (boneNode.Bone.ParentIndex is int parentIndex && nodesByBoneIndex.TryGetValue(parentIndex, out var parentBoneNode))
                {
                    parentBoneNode.AddChild(boneNode);
                }
                else
                {
                    parentNode.AddChild(boneNode);
                }
            }

            skinning.Skeleton.UpdateWorldTransforms();

            foreach (var bone in skinning.Skeleton.Bones)
            {
                if (bone.ParentIndex is not int parentIndex)
                {
                    continue;
                }

                var parentPosition = skinning.Skeleton.Bones[parentIndex].WorldTransform.ExtractTranslation();
                var childPosition = bone.WorldTransform.ExtractTranslation();
                var vertexIndex = (uint)lineVertices.Count;

                lineVertices.Add(new Vertex(parentPosition));
                lineVertices.Add(new Vertex(childPosition));
                lineIndices.Add(vertexIndex);
                lineIndices.Add(vertexIndex + 1);
            }

            if (lineVertices.Count == 0)
            {
                return;
            }

            _skeletonLineNode = new MeshNode
            {
                Mesh = new Mesh([.. lineVertices], [.. lineIndices])
                {
                    Topology = MeshTopology.Lines
                },
                Color = new Vector3(0.15f, 0.9f, 1f),
                IsVisible = isVisible,
                ForceWireframe = true
            };

            parentNode.AddChild(_skeletonLineNode);
        }

        public void SyncToSkeleton()
        {
            foreach (var boneNode in _boneNodes)
            {
                boneNode.ApplyBoneTransform();
            }

            UpdateLines();
        }

        public void UpdateLines()
        {
            if (_activeParentNode?.Mesh?.Skinning is not { } skinning || _skeletonLineNode?.Mesh is not { } lineMesh)
            {
                return;
            }

            skinning.Skeleton.UpdateWorldTransforms();

            var lineVertexIndex = 0;

            foreach (var bone in skinning.Skeleton.Bones)
            {
                if (bone.ParentIndex is not int parentIndex || lineVertexIndex + 1 >= lineMesh.Vertices.Length)
                {
                    continue;
                }

                lineMesh.Vertices[lineVertexIndex].Position = skinning.Skeleton.Bones[parentIndex].WorldTransform.ExtractTranslation();
                lineVertexIndex++;

                lineMesh.Vertices[lineVertexIndex].Position = bone.WorldTransform.ExtractTranslation();
                lineVertexIndex++;
            }

            _skeletonLineNode.ApplyDeformers();
        }

        public void SetVisibility(bool isVisible)
        {
            foreach (var boneNode in _boneNodes)
            {
                boneNode.IsVisible = isVisible;
            }

            _skeletonLineNode?.IsVisible = isVisible;

            if (!isVisible && gizmoSystem.TargetNode is BoneNode)
            {
                gizmoSystem.TargetNode = null;
            }
        }

        public void Clear(SceneNode? fallbackTargetNode)
        {
            foreach (var boneNode in _boneNodes)
            {
                boneNode.Parent?.RemoveChild(boneNode);
            }

            _boneNodes.Clear();
            gizmoSystem.BoneGizmoRadius = 0f;
            _skeletonLineNode?.Parent?.RemoveChild(_skeletonLineNode);
            _skeletonLineNode = null;

            if (gizmoSystem.TargetNode is BoneNode)
            {
                gizmoSystem.TargetNode = fallbackTargetNode;
            }

            _activeParentNode = null;
        }

        #endregion
    }
}