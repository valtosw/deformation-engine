using Deformation.Abstractions.Geometry;
using Deformation.Scene.Nodes;

namespace Deformation.Scene.Abstractions
{
    public interface ISkeletonVisualBuilder
    {
        IReadOnlyList<BoneNode> BoneNodes { get; }

        void Build(MeshNode parentNode, Mesh mesh, float targetSphereRadius, bool isVisible);
        void SyncToSkeleton();
        void UpdateLines();
        void SetVisibility(bool isVisible);
        void Clear(SceneNode? fallbackTargetNode);
    }
}