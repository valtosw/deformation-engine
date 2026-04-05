using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Visualization.Abstractions.Geometry;
using Visualization.Rendering.Abstractions;

namespace Visualization.Rendering
{
    public sealed class GlRenderingContext(Shader shader) : IRenderingContext
    {
        private readonly Dictionary<int, MeshBuffer> _buffers = [];
        private int _nextBufferId = 1;

        public void BeginFrame()
        {
            GL.ClearColor(0.1f, 0.1f, 0.12f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);
            shader.Use();
        }

        public void SetMatrix(string name, Matrix4 matrix)
        {
            shader.SetMatrix4(name, matrix);
        }

        public void SetVector(string name, Vector3 vector)
        {
            shader.SetVector3(name, vector);
        }

        public int CreateBuffer(Mesh mesh)
        {
            var buffer = new MeshBuffer(mesh);
            var bufferId = _nextBufferId++;
            _buffers[bufferId] = buffer;

            return bufferId;
        }

        public void DeleteBuffer(int bufferId)
        {
            if (!_buffers.TryGetValue(bufferId, out var buffer))
                return;

            buffer.Dispose();
            _buffers.Remove(bufferId);
        }

        public void DrawBuffer(int bufferId)
        {
            _buffers[bufferId].Draw();
        }

        public void UpdateBuffer(int bufferId, Mesh mesh)
        {
            _buffers[bufferId].Update(mesh);
        }
    }
}
