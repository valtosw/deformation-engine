using OpenTK.Mathematics;

namespace Deformation.Abstractions.Constants
{
    public static class ColorConstants
    {
        public static readonly Vector3 XAxisColor = new(1f, 0.2f, 0.2f);
        public static readonly Vector3 YAxisColor = new(0.2f, 1f, 0.2f);
        public static readonly Vector3 ZAxisColor = new(0.2f, 0.5f, 1f);

        public static readonly Vector3 DefaultObjectColor = new(0.6f, 0.6f, 0.6f);
    }
}
