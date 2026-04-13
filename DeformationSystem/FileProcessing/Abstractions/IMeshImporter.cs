using Visualization.Abstractions.Geometry;

namespace FileProcessing.Abstractions
{
    public interface IMeshImporter
    {
        string[] SupportedExtensions { get; }

        Mesh Load(Stream stream);
    }
}
