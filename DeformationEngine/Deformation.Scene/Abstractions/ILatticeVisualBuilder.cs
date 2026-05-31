using Deformation.Modifiers.Deformers;
using Deformation.Scene.Nodes;

namespace Deformation.Scene.Abstractions
{
    public interface ILatticeVisualBuilder
    {
        IReadOnlyList<ControlPointNode> ControlPointNodes { get; }

        void Build(MeshNode parentNode, FfdDeformer deformer, float targetSphereRadius, bool isVisible, Action onLatticeChanged);
        void UpdateFromLattice(FfdDeformer deformer);
        void SetVisibility(bool isVisible);
        void Clear();
    }
}