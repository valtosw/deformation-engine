using OpenTK.Mathematics;
using Visualization.Abstractions.Geometry;

namespace Visualization.Rendering.Abstractions
{
    public interface IRenderingContext
    {
        void BeginFrame();

        void SetMatrix(string name, Matrix4 matrix);
        void SetVector(string name, Vector3 vector);

        int CreateBuffer(Mesh mesh);
        void DrawBuffer(int bufferId);
        void DeleteBuffer(int bufferId);
        void UpdateBuffer(int bufferId, Mesh mesh);
    }
}
