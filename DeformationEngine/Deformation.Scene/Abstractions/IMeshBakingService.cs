using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Nodes;

namespace Deformation.Scene.Abstractions
{
    public interface IMeshBakingService
    {
        Mesh BakeMesh(MeshNode meshNode, TwistDeformer twistDeformer, BendDeformer bendDeformer, FfdDeformer ffdDeformer);
    }
}