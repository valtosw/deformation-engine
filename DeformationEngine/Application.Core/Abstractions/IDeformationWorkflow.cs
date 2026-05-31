using Deformation.Abstractions.Enums;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Nodes;

namespace Application.Core.Abstractions
{
    public interface IDeformationWorkflow
    {
        TwistDeformer TwistDeformer { get; }
        BendDeformer BendDeformer { get; }
        FfdDeformer FfdDeformer { get; }
        LbsDeformer LbsDeformer { get; }

        void AttachDeformers(MeshNode meshNode);
        void SetupFfdLattice(MeshNode meshNode, int resolutionX, int resolutionY, int resolutionZ, float sphereRadius, bool isVisible);
        void SubdivideMesh(MeshNode meshNode, int resolutionX, int resolutionY, int resolutionZ, float sphereRadius, DeformationMode currentMode);
        void BakeTransformations(MeshNode meshNode, int resolutionX, int resolutionY, int resolutionZ, float sphereRadius, DeformationMode currentMode);
        void RestoreParameters(MeshNode meshNode, DeformationMode currentMode);
        void ApplyDeformations(MeshNode? meshNode);
        void SetLbsEnabled(bool isEnabled, MeshNode? meshNode);
        bool HasUnbakedChanges(MeshNode meshNode, DeformationMode currentMode);
    }
}