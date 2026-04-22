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
            IStlParser parser = IsAsciiStl(stream)
                ? new AsciiStlParser()
                : new BinaryStlParser();

            return parser.Parse(stream);
        }

        private static bool IsAsciiStl(Stream stream)
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var isAscii = reader.ReadLine()?.TrimStart().StartsWith("solid", StringComparison.OrdinalIgnoreCase) == true;
            stream.Position = 0;

            return isAscii;
        }
    }
}
