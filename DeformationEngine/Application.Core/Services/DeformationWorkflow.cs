using Application.Core.Abstractions;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;

namespace Application.Core.Services
{
    public sealed class DeformationWorkflow : IDeformationWorkflow
    {
        #region Fields

        private readonly IMeshBakingService _meshBakingService;
        private readonly ILatticeVisualBuilder _latticeBuilder;
        private readonly ISkeletonVisualBuilder _skeletonBuilder;

        #endregion

        #region Constructors

        public DeformationWorkflow(
            IMeshBakingService meshBakingService,
            ILatticeVisualBuilder latticeBuilder,
            ISkeletonVisualBuilder skeletonBuilder)
        {
            _meshBakingService = meshBakingService;
            _latticeBuilder = latticeBuilder;
            _skeletonBuilder = skeletonBuilder;
        }

        #endregion

        #region Properties

        public TwistDeformer TwistDeformer { get; } = new();
        public BendDeformer BendDeformer { get; } = new();
        public FfdDeformer FfdDeformer { get; } = new();
        public LbsDeformer LbsDeformer { get; } = new();

        #endregion

        #region Public Logic

        public void AttachDeformers(MeshNode meshNode)
        {
            meshNode.AddDeformer(TwistDeformer);
            meshNode.AddDeformer(BendDeformer);
            meshNode.AddDeformer(FfdDeformer);
            meshNode.AddDeformer(LbsDeformer);
        }

        public void SetupFfdLattice(MeshNode meshNode, int resolutionX, int resolutionY, int resolutionZ, float sphereRadius, bool isVisible)
        {
            if (meshNode.Mesh is null)
            {
                return;
            }

            _latticeBuilder.Clear();
            FfdDeformer.Initialize(meshNode.Mesh, resolutionX, resolutionY, resolutionZ);

            _latticeBuilder.Build(
                meshNode,
                FfdDeformer,
                sphereRadius,
                isVisible,
                meshNode.ApplyDeformers
            );

            meshNode.ApplyDeformers();
        }

        public void SubdivideMesh(MeshNode meshNode, int resolutionX, int resolutionY, int resolutionZ, float sphereRadius, DeformationMode currentMode)
        {
            if (meshNode.Mesh is null)
            {
                return;
            }

            var newMesh = meshNode.Mesh.Subdivide();

            _latticeBuilder.Clear();
            FfdDeformer.Clear();

            meshNode.Mesh = newMesh;

            if (currentMode == DeformationMode.Ffd)
            {
                SetupFfdLattice(meshNode, resolutionX, resolutionY, resolutionZ, sphereRadius, true);
            }
        }

        public void BakeTransformations(MeshNode meshNode, int resolutionX, int resolutionY, int resolutionZ, float sphereRadius, DeformationMode currentMode)
        {
            meshNode.ProcessPendingDeformations();
            var bakedMesh = _meshBakingService.BakeMesh(meshNode, TwistDeformer, BendDeformer, FfdDeformer);

            meshNode.Translation = OpenTK.Mathematics.Vector3.Zero;
            meshNode.Rotation = OpenTK.Mathematics.Quaternion.Identity;
            meshNode.Scale = OpenTK.Mathematics.Vector3.One;

            TwistDeformer.Angle = 0f;
            BendDeformer.Angle = 0f;

            _latticeBuilder.Clear();
            FfdDeformer.Clear();

            if (meshNode.Mesh?.Skinning is { } updatedSkinning)
            {
                updatedSkinning.Skeleton.RebindToCurrentPose();
                _skeletonBuilder.SyncToSkeleton();
                LbsDeformer.IsEnabled = currentMode == DeformationMode.LinearBlendSkinning;
            }

            meshNode.Mesh = bakedMesh;

            if (currentMode == DeformationMode.Ffd)
            {
                SetupFfdLattice(meshNode, resolutionX, resolutionY, resolutionZ, sphereRadius, true);
            }
        }

        public void RestoreParameters(MeshNode meshNode, DeformationMode currentMode)
        {
            meshNode.Translation = OpenTK.Mathematics.Vector3.Zero;
            meshNode.Rotation = OpenTK.Mathematics.Quaternion.Identity;
            meshNode.Scale = OpenTK.Mathematics.Vector3.One;

            if (currentMode == DeformationMode.Ffd && FfdDeformer.IsInitialized)
            {
                FfdDeformer.Reset();
                _latticeBuilder.UpdateFromLattice(FfdDeformer);
            }

            if (meshNode.Mesh?.Skinning is { } skinning)
            {
                skinning.Skeleton.ResetToBindPose();
                _skeletonBuilder.SyncToSkeleton();
            }

            meshNode.ApplyDeformers();
        }

        public void ApplyDeformations(MeshNode? meshNode)
        {
            meshNode?.ApplyDeformers();
        }

        public void SetLbsEnabled(bool isEnabled, MeshNode? meshNode)
        {
            if (LbsDeformer.IsEnabled == isEnabled)
            {
                return;
            }

            LbsDeformer.IsEnabled = isEnabled;
            meshNode?.ApplyDeformers();
        }

        public bool HasUnbakedChanges(MeshNode meshNode, DeformationMode currentMode)
        {
            return currentMode switch
            {
                _ when currentMode == DeformationMode.Basic => meshNode.Translation != OpenTK.Mathematics.Vector3.Zero ||
                                                                meshNode.Rotation != OpenTK.Mathematics.Quaternion.Identity ||
                                                                meshNode.Scale != OpenTK.Mathematics.Vector3.One,
                _ when currentMode == DeformationMode.Twist => Math.Abs(TwistDeformer.Angle) > MathConstants.ZeroTolerance,
                _ when currentMode == DeformationMode.Bend => Math.Abs(BendDeformer.Angle) > MathConstants.ZeroTolerance,
                _ when currentMode == DeformationMode.Ffd => FfdDeformer.HasChanges,
                _ when currentMode == DeformationMode.LinearBlendSkinning => LbsDeformer.IsEnabled && HasSkeletonChanges(meshNode),
                _ => false
            };
        }

        #endregion

        #region Private Logic

        private static bool HasSkeletonChanges(MeshNode meshNode)
        {
            if (meshNode.Mesh?.Skinning is not { } skinning)
            {
                return false;
            }

            return skinning.Skeleton.Bones.Any(bone => !bone.LocalTransform.IsClose(bone.BindLocalTransform));
        }

        #endregion
    }
}