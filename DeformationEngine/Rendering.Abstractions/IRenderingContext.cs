using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;

namespace Rendering.Abstractions
{
    public interface IRenderingContext : IDisposable
    {
        bool IsWireframeEnabled { get; set; }

        void BeginFrame();

        void SetMatrix(string name, Matrix4 matrix);
        void SetVector(string name, Vector3 vector);
        void SetWireframeOverride(bool? isWireframe);

        int CreateBuffer(Mesh mesh);
        void DrawBuffer(int bufferId, bool ignoreDepth);
        void DeleteBuffer(int bufferId);
        void UpdateBuffer(int bufferId, Mesh mesh);
    }
}