using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Net.NetworkInformation;

namespace Visualization.Rendering
{
    public sealed class Shader : IDisposable
    {
        private readonly int _handle;

        public Shader(string vertexResourceName, string fragmentResourceName)
        {
            var vertexSource = ReadEmbeddedResource(vertexResourceName);
            var fragmentSource = ReadEmbeddedResource(fragmentResourceName);

            var vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            _handle = GL.CreateProgram();
            GL.AttachShader(_handle, vertexShader);
            GL.AttachShader(_handle, fragmentShader);
            GL.LinkProgram(_handle);

            GL.DetachShader(_handle, vertexShader);
            GL.DetachShader(_handle, fragmentShader);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        public void Dispose()
        {
            GL.DeleteProgram(_handle);
        }

        public void Use()
        {
            GL.UseProgram(_handle);
        }

        public void SetMatrix4(string name, Matrix4 matrix)
        {
            var location = GL.GetUniformLocation(_handle, name);
            GL.UniformMatrix4(location, transpose: false, ref matrix);
        }

        public void SetVector3(string name, Vector3 vector)
        {
            var location = GL.GetUniformLocation(_handle, name);
            GL.Uniform3(location, vector);
        }

        public void SetBool(string name, bool value)
        {
            var location = GL.GetUniformLocation(_handle, name);
            GL.Uniform1(location, value ? 1 : 0);
        }

        private static int CompileShader(ShaderType type, string source)
        {
            var shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            return shader;
        }

        private static string ReadEmbeddedResource(string name)
        {
            var assembly = typeof(Shader).Assembly;
            var resourceName = $"Visualization.Rendering.Shaders.{name}";

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Resource '{resourceName}' not found.");

            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
