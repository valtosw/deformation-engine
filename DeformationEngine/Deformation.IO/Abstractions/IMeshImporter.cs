using Deformation.Abstractions.Geometry;

namespace Deformation.IO.Abstractions
{
    public interface IMeshImporter
    {
        string[] SupportedExtensions { get; }

        Mesh Load(Stream stream);
    }
}
