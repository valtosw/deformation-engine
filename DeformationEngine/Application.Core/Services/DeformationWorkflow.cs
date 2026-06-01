using Application.Core.Abstractions;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Modifiers.Abstractions;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Core.Services
{
    public sealed class DeformationWorkflow(
        IMeshBakingService meshBakingService,
        ILatticeVisualBuilder latticeBuilder,
        IArapSelectionVisualBuilder arapSelectionBuilder,
        ISkeletonVisualBuilder skeletonBuilder,
        IEnumerable<IDeformer> deformers) : IDeformationWorkflow
    {

        #region Fields

        private readonly Dictionary<Type, IDeformer> _deformers = deformers.ToDictionary(deformer => deformer.GetType());

        #endregion

        #region Properties

        public IEnumerable<IDeformer> Deformers => _deformers.Values;

        #endregion

        #region Public Logic

        public T GetDeformer<T>() where T : class, IDeformer
        {
            if (_deformers.TryGetValue(typeof(T), out var deformer))
            {
                return (T)deformer;
            }

            throw new InvalidOperationException($"Deformer of type {typeof(T).Name} is not registered.");
        }

        public void AttachDeformers(MeshNode meshNode)
        {
            foreach (var deformer in _deformers.Values)
            {
                meshNode.AddDeformer(deformer);
            }
        }

        public void SetupFfdLattice(MeshNode meshNode, int resolutionX, int resolutionY, int resolutionZ, float sphereRadius, bool isVisible)
        {
            if (meshNode.Mesh is null)
            {
                return;
            }

            latticeBuilder.Clear();

            var ffdDeformer = GetDeformer<FfdDeformer>();
            ffdDeformer.Initialize(meshNode.Mesh, resolutionX, resolutionY, resolutionZ);

            latticeBuilder.Build(
                meshNode,
                ffdDeformer,
                sphereRadius,
                isVisible,
                meshNode.ApplyDeformers
            );

            meshNode.ApplyDeformers();
        }

        public void SetupArapSelection(MeshNode meshNode, float sphereRadius, bool isVisible)
        {
            if (meshNode.Mesh is null)
            {
                return;
            }

            arapSelectionBuilder.Clear();

            var arapDeformer = GetDeformer<ArapDeformer>();
            arapDeformer.Initialize(meshNode.Mesh);

            arapSelectionBuilder.Build(
                meshNode,
                arapDeformer,
                sphereRadius,
                isVisible,
                meshNode.ApplyDeformers);

            meshNode.ApplyDeformers();
        }

        public void SubdivideMesh(MeshNode meshNode, int resolutionX, int resolutionY, int resolutionZ, float sphereRadius, DeformationMode currentMode)
        {
            if (meshNode.Mesh is null)
            {
                return;
            }

            var newMesh = meshNode.Mesh.Subdivide();

            latticeBuilder.Clear();
            GetDeformer<FfdDeformer>().Clear();
            arapSelectionBuilder.Clear();
            GetDeformer<ArapDeformer>().Clear();

            meshNode.Mesh = newMesh;

            if (currentMode == DeformationMode.FreeFormDeformation)
            {
                SetupFfdLattice(meshNode, resolutionX, resolutionY, resolutionZ, sphereRadius, true);
            }
            else if (currentMode == DeformationMode.AsRigidAsPossible)
            {
                SetupArapSelection(meshNode, sphereRadius, true);
            }
        }

        public void BakeTransformations(MeshNode meshNode, int resolutionX, int resolutionY, int resolutionZ, float sphereRadius, DeformationMode currentMode)
        {
            meshNode.ProcessPendingDeformations();

            var twistDeformer = GetDeformer<TwistDeformer>();
            var bendDeformer = GetDeformer<BendDeformer>();
            var ffdDeformer = GetDeformer<FfdDeformer>();
            var arapDeformer = GetDeformer<ArapDeformer>();

            var bakedMesh = meshBakingService.BakeMesh(meshNode, twistDeformer, bendDeformer, ffdDeformer);

            meshNode.Translation = OpenTK.Mathematics.Vector3.Zero;
            meshNode.Rotation = OpenTK.Mathematics.Quaternion.Identity;
            meshNode.Scale = OpenTK.Mathematics.Vector3.One;

            twistDeformer.Angle = 0f;
            bendDeformer.Angle = 0f;

            latticeBuilder.Clear();
            ffdDeformer.Clear();
            arapSelectionBuilder.Clear();
            arapDeformer.Clear();

            if (meshNode.Mesh?.Skinning is { } updatedSkinning)
            {
                updatedSkinning.Skeleton.RebindToCurrentPose();
                skeletonBuilder.SyncToSkeleton();
                GetDeformer<LbsDeformer>().IsEnabled = currentMode == DeformationMode.LinearBlendSkinning;
            }

            meshNode.Mesh = bakedMesh;

            if (currentMode == DeformationMode.FreeFormDeformation)
            {
                SetupFfdLattice(meshNode, resolutionX, resolutionY, resolutionZ, sphereRadius, true);
            }
            else if (currentMode == DeformationMode.AsRigidAsPossible)
            {
                SetupArapSelection(meshNode, sphereRadius, true);
            }
        }

        public void RestoreParameters(MeshNode meshNode, DeformationMode currentMode)
        {
            meshNode.Translation = OpenTK.Mathematics.Vector3.Zero;
            meshNode.Rotation = OpenTK.Mathematics.Quaternion.Identity;
            meshNode.Scale = OpenTK.Mathematics.Vector3.One;

            var ffdDeformer = GetDeformer<FfdDeformer>();
            var arapDeformer = GetDeformer<ArapDeformer>();

            if (currentMode == DeformationMode.FreeFormDeformation && ffdDeformer.IsInitialized)
            {
                ffdDeformer.Reset();
                latticeBuilder.UpdateFromLattice(ffdDeformer);
            }

            if (meshNode.Mesh?.Skinning is { } skinning)
            {
                skinning.Skeleton.ResetToBindPose();
                skeletonBuilder.SyncToSkeleton();
            }

            if (currentMode == DeformationMode.AsRigidAsPossible)
            {
                arapDeformer.Reset();
            }

            meshNode.ApplyDeformers();
        }

        public void ApplyDeformations(MeshNode? meshNode)
        {
            meshNode?.ApplyDeformers();
        }

        public void SetLbsEnabled(bool isEnabled, MeshNode? meshNode)
        {
            var lbsDeformer = GetDeformer<LbsDeformer>();

            if (lbsDeformer.IsEnabled == isEnabled)
            {
                return;
            }

            lbsDeformer.IsEnabled = isEnabled;
            meshNode?.ApplyDeformers();
        }

        public bool HasUnbakedChanges(MeshNode meshNode, DeformationMode currentMode)
        {
            return currentMode switch
            {
                _ when currentMode == DeformationMode.Basic => meshNode.Translation != OpenTK.Mathematics.Vector3.Zero ||
                                                               meshNode.Rotation != OpenTK.Mathematics.Quaternion.Identity ||
                                                               meshNode.Scale != OpenTK.Mathematics.Vector3.One,
                _ when currentMode == DeformationMode.Twist => Math.Abs(GetDeformer<TwistDeformer>().Angle) > MathConstants.ZeroTolerance,
                _ when currentMode == DeformationMode.Bend => Math.Abs(GetDeformer<BendDeformer>().Angle) > MathConstants.ZeroTolerance,
                _ when currentMode == DeformationMode.FreeFormDeformation => GetDeformer<FfdDeformer>().HasChanges,
                _ when currentMode == DeformationMode.AsRigidAsPossible => GetDeformer<ArapDeformer>().HasChanges,
                _ when currentMode == DeformationMode.LinearBlendSkinning => GetDeformer<LbsDeformer>().IsEnabled && HasSkeletonChanges(meshNode),
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
