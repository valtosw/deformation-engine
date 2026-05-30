using System.Text;
using Deformation.Abstractions.Geometry;
using Deformation.IO.Abstractions;
using Deformation.IO.Constants;
using Deformation.IO.Importers.Parsers;

namespace Deformation.IO.Importers
{
    public sealed class StlMeshImporter : IMeshImporter
    {
        public string[] SupportedExtensions => [ImporterConstants.Stl.Extension];

        public Mesh Load(string filePath)
        {
            using var stream = File.OpenRead(filePath);
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
