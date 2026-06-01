using Deformation.Modifiers.Deformers;
using Deformation.Scene.Nodes;

namespace Deformation.Scene.Abstractions
{
    public interface IArapSelectionVisualBuilder
    {
        ArapHandleNode? HandleNode { get; }

        void Build(MeshNode parentNode, ArapDeformer deformer, float targetSphereRadius, bool isVisible, Action onHandleChanged);
        void Refresh();
        void SetVisibility(bool isVisible);
        void Clear();
    }
}
