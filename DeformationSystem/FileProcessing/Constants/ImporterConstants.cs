namespace FileProcessing.Constants
{
    internal static class ImporterConstants
    {
        public static class Stl
        {
            public const string Extension = ".stl";
            public const int HeaderSize = 80;
            public const int TriangleCountSize = 4;
            public const int MinimumBinaryFileSize = HeaderSize + TriangleCountSize;
            public const int BytesPerTriangle = 50;
            public const int VerticesPerTriangle = 3;
        }

        public static class Obj
        {
            public const string Extension = ".obj";
        }
    }
}
