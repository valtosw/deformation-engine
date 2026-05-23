using Deformation.Abstractions.Geometry;

namespace Deformation.Modifiers.Abstractions
{
    public interface IDeformer
    {
        void Deform(Mesh mesh);
    }
}
