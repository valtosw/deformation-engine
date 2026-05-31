using Deformation.Abstractions.Enums;
using Deformation.Modifiers.Abstractions;
using Deformation.Scene.Nodes;
using System.Collections.Generic;

namespace Application.Core.Abstractions
{
    public interface IDeformationWorkflow
    {
        IEnumerable<IDeformer> Deformers { get; }

        T GetDeformer<T>() where T : class, IDeformer;

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