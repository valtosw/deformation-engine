using Visualization.Abstractions.Geometry;

namespace FileProcessing.Abstractions
{
    public interface IStlParser
    {
        Mesh Parse(Stream stream);
    }
}
