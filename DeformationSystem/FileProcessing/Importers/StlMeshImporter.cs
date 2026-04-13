using System.Text;
using FileProcessing.Abstractions;
using FileProcessing.Importers.Parsers;
using FileProcessing.Constants;
using Visualization.Abstractions.Geometry;

namespace FileProcessing.Importers
{
    public sealed class StlMeshImporter : IMeshImporter
    {
        public string[] SupportedExtensions => [ImporterConstants.Stl.Extension];

        public Mesh Load(Stream stream)
        {
            if (IsBinaryStl(stream))
            {
                var binaryParser = new BinaryStlParser(stream);
                return binaryParser.Parse();
            }

            using var streamReader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var asciiParser = new AsciiStlParser(streamReader);
            return asciiParser.Parse();
        }

        private static bool IsBinaryStl(Stream stream)
        {
            if (stream.Length < ImporterConstants.Stl.MinimumBinaryFileSize)
                return false;

            stream.Position = ImporterConstants.Stl.HeaderSize;
            
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var triangleCount = reader.ReadUInt32();

            var expectedSize = ImporterConstants.Stl.MinimumBinaryFileSize + (triangleCount * ImporterConstants.Stl.BytesPerTriangle);
            stream.Position = 0;

            return stream.Length == expectedSize;
        }
    }
}
