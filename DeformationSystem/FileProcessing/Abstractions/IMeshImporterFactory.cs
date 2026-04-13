namespace FileProcessing.Abstractions
{
    public interface IMeshImporterFactory
    {
        IMeshImporter GetImporter(string extension);
    }
}
