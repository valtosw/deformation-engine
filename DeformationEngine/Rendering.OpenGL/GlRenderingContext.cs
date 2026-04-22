using Deformation.Abstractions.Geometry;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Rendering.Abstractions;

namespace Rendering.OpenGL
{
    public sealed class GlRenderingContext(Shader shader) : IRenderingContext
    {
        #region Fields

        private readonly Dictionary<int, MeshBuffer> _buffers = [];
        private int _nextBufferId = 1;

        #endregion

        #region Properties

        public bool IsWireframeEnabled { get; set; }

        #endregion

        #region Public Logic

        public void Dispose()
        {
            foreach (var buffer in _buffers.Values)
            {
                buffer.Dispose();
            }

            _buffers.Clear();
            shader.Dispose();
        }

        public void BeginFrame()
        {
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.DepthTest);

            GL.PolygonMode(TriangleFace.FrontAndBack, IsWireframeEnabled ? PolygonMode.Line : PolygonMode.Fill);

            shader.Use();
            shader.SetBool("isWireframe", IsWireframeEnabled);
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
            {
                return;
            }

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

        #endregion
    }
}
