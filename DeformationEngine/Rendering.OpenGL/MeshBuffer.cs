using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using OpenTK.Graphics.OpenGL4;

namespace Rendering.OpenGL
{
    internal sealed class MeshBuffer : IDisposable
    {
        #region Fields

        private readonly int _vertexBufferObject;
        private readonly int _elementBufferObject;
        private readonly int _vertexArrayObject;

        private int _indexCount;
        private int _vertexCount;
        private MeshTopology _topology;

        #endregion

        #region Constructors

        public MeshBuffer(Mesh mesh)
        {
            _vertexBufferObject = GL.GenBuffer();
            _elementBufferObject = GL.GenBuffer();
            _vertexArrayObject = GL.GenVertexArray();

            Update(mesh);
        }

        #endregion

        #region Public Logic

        public void Dispose()
        {
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteBuffer(_elementBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);
        }

        public void Update(Mesh mesh)
        {
            _topology = mesh.Topology;

            GL.BindVertexArray(_vertexArrayObject);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);

            if (_vertexCount != mesh.Vertices.Length || _indexCount != mesh.Indices.Length)
            {
                _vertexCount = mesh.Vertices.Length;
                _indexCount = mesh.Indices.Length;

                GL.BufferData(BufferTarget.ArrayBuffer, size: mesh.Vertices.Length * 32, mesh.Vertices, BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
                GL.BufferData(BufferTarget.ElementArrayBuffer, size: mesh.Indices.Length * sizeof(uint), mesh.Indices, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(index: 0, size: 3, type: VertexAttribPointerType.Float, normalized: false, stride: 32, offset: 0);
                GL.EnableVertexAttribArray(index: 0);

                GL.VertexAttribPointer(index: 1, size: 3, type: VertexAttribPointerType.Float, normalized: false, stride: 32, offset: 12);
                GL.EnableVertexAttribArray(index: 1);
            }
            else
            {
                GL.BufferSubData(BufferTarget.ArrayBuffer, offset: IntPtr.Zero, size: mesh.Vertices.Length * 32, data: mesh.Vertices);
            }

            GL.BindVertexArray(0);
        }

        public void Draw()
        {
            GL.BindVertexArray(_vertexArrayObject);

            var primitiveType = _topology == MeshTopology.Triangles
                ? PrimitiveType.Triangles
                : PrimitiveType.Lines;

            GL.DrawElements(primitiveType, _indexCount, DrawElementsType.UnsignedInt, indices: 0);
        }

        #endregion
    }
}