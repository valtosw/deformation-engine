namespace Deformation.IO.Abstractions
{
    public interface IMeshImporterFactory
    {
        IMeshImporter GetImporter(string extension);
    }
}
