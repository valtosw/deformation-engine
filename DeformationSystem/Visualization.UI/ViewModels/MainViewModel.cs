using OpenTK.Mathematics;
using Visualization.Abstractions.Geometry;
using Visualization.Interaction;
using Visualization.Interaction.Input;
using Visualization.Rendering.Abstractions;
using Visualization.Scene.Abstractions;
using Visualization.Scene.Nodes;

namespace Visualization.UI.ViewModels
{
    public sealed class MainViewModel
    {
        public MainViewModel(VisualizationEngine engine, ICameraSystem cameraSystem)
        {
            Engine = engine;
            CameraSystem = cameraSystem;

            Engine.RegisterController(new CameraKeyboardController(CameraSystem, Engine));
            Engine.RegisterController(new CameraMouseController(CameraSystem));
        }

        public VisualizationEngine Engine { get; }
        public ICameraSystem CameraSystem { get; }

        public void InitializeRendering(IRenderingContext renderingContext)
        {
            Engine.Initialize(renderingContext);
            CreateTestScene();
        }

        public void Resize(int width, int height)
        {
            CameraSystem.SetViewport(width, height);
        }

        public void Render(float deltaTime)
        {
            Engine.UpdateAndRender(deltaTime, CameraSystem.ViewMatrix, CameraSystem.ProjectionMatrix);
        }

        public void ProcessInput(IInputEvent inputEvent)
        {
            Engine.ProcessInput(inputEvent);
        }

        private void CreateTestScene()
        {
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
            Engine.RootNode.AddChild(cubeNode);
        }
    }
}
