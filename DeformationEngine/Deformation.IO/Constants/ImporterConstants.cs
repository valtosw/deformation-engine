namespace Deformation.IO.Constants
{
    internal static class ImporterConstants
    {
        public static class Stl
        {
            public const string Extension = ".stl";
            public const int HeaderSize = 80;
            public const int VerticesPerTriangle = 3;
        }

        public static class Obj
        {
            public const string Extension = ".obj";
        }

        public static class Gltf
        {
            public const string TextExtension = ".gltf";
            public const string BinaryExtension = ".glb";
        }

        public static class Collada
        {
            public const string Extension = ".dae";
        }
    }
}
