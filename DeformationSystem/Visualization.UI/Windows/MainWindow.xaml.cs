using System.IO;
using System.Windows;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System.Windows.Input;
using Visualization.Abstractions.Geometry;
using Visualization.Interaction;
using Visualization.Interaction.Input;
using Visualization.Rendering;
using Visualization.Scene.Nodes;

namespace Visualization.UI.Windows
{
    public sealed partial class MainWindow
    {
        private readonly SceneController _sceneController = new();

        public MainWindow()
        {
            InitializeComponent();

            GlRenderingControl.Start(new GLWpfControlSettings
            {
                MajorVersion = 3,
                MinorVersion = 3
            });
        }

        private void GlRenderingControl_OnReady()
        {
            var shadersDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Shaders");
            var shader = new Shader(
                Path.Combine(shadersDir, "default.vert"),
                Path.Combine(shadersDir, "default.frag"));

            var renderingContext = new GlRenderingContext(shader);
            _sceneController.Initialize(renderingContext);
            _sceneController.OnViewportResize((int)GlRenderingControl.ActualWidth, (int)GlRenderingControl.ActualHeight);

            // Hardcoded test cube
            var mesh = new Mesh(
            [
                // Front face
                new Vertex(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(0, 0, 1)),
                new Vertex(new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(0, 0, 1)),
                new Vertex(new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(0, 0, 1)),
                new Vertex(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(0, 0, 1)),
                // Back face
                new Vertex(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(0, 0, -1)),
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0, 0, -1)),
                new Vertex(new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(0, 0, -1)),
                new Vertex(new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(0, 0, -1)),
                // Top face
                new Vertex(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(0, 1, 0)),
                new Vertex(new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(0, 1, 0)),
                new Vertex(new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(0, 1, 0)),
                new Vertex(new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(0, 1, 0)),
                // Bottom face
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0, -1, 0)),
                new Vertex(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(0, -1, 0)),
                new Vertex(new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(0, -1, 0)),
                new Vertex(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(0, -1, 0)),
                // Right face
                new Vertex(new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(1, 0, 0)),
                new Vertex(new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(1, 0, 0)),
                new Vertex(new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(1, 0, 0)),
                new Vertex(new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(1, 0, 0)),
                // Left face
                new Vertex(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-1, 0, 0)),
                new Vertex(new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-1, 0, 0)),
                new Vertex(new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(-1, 0, 0)),
                new Vertex(new Vector3(-0.5f,  0.5f, -0.5f), new Vector3(-1, 0, 0)),
            ],
            [
                 0,  1,  2,   2,  3,  0, // Front
                 4,  5,  6,   6,  7,  4, // Back
                 8,  9, 10,  10, 11,  8, // Top
                12, 13, 14,  14, 15, 12, // Bottom
                16, 17, 18,  18, 19, 16, // Right
                20, 21, 22,  22, 23, 20, // Left
            ]);

            var cubeNode = new MeshNode { Mesh = mesh };
            _sceneController.RootNode.AddChild(cubeNode);
        }

        private void GlRenderingControl_OnRender(TimeSpan delta) => _sceneController.Render((float)delta.TotalSeconds);

        private void GlRenderingControl_OnSizeChanged(object sender, SizeChangedEventArgs e)
            => _sceneController.OnViewportResize((int)e.NewSize.Width, (int)e.NewSize.Height);

        private void GlRenderingControl_OnMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(GlRenderingControl);
            _sceneController.ProcessInput(
                new MouseMoveEvent(new Vector2((float)position.X, (float)position.Y)),
                    e.RightButton == MouseButtonState.Pressed, e.MiddleButton == MouseButtonState.Pressed);
        }

        private void GlRenderingControl_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(GlRenderingControl);

            var button = e.ChangedButton switch
            {
                System.Windows.Input.MouseButton.Left => Interaction.Input.MouseButton.Left,
                System.Windows.Input.MouseButton.Right => Interaction.Input.MouseButton.Right,
                _ => Interaction.Input.MouseButton.Middle
            };

            _sceneController.ProcessInput(
                new MouseClickEvent(new Vector2((float)position.X, (float)position.Y), 
                    button, Interaction.Input.InputType.Down), false, false);
        }

        private void GlRenderingControl_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var position = e.GetPosition(GlRenderingControl);
            _sceneController.ProcessInput(new MouseWheelEvent(new Vector2((float)position.X, (float)position.Y), e.Delta), false, false);
        }
    }
}
