using Deformation.IO.Abstractions;

namespace Deformation.IO
{
    public sealed class MeshImporterFactory : IMeshImporterFactory
    {
        private readonly Dictionary<string, IMeshImporter> _importers = new(StringComparer.OrdinalIgnoreCase);

        public MeshImporterFactory(IEnumerable<IMeshImporter> importers)
        {
            foreach (var importer in importers)
            {
                foreach (var extension in importer.SupportedExtensions)
                {
                    _importers[extension] = importer;
                }
            }
        }

        public IMeshImporter GetImporter(string extension)
        {
            if (!_importers.TryGetValue(extension, out var importer))
            {
                throw new NotSupportedException($"File extension '{extension}' is not supported.");
            }

            return importer;
        }
    }
}
