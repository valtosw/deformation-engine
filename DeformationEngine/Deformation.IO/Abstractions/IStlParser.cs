using Deformation.Abstractions.Geometry;

namespace Deformation.IO.Abstractions
{
    public interface IStlParser
    {
        Mesh Parse(Stream stream);
    }
}
