using FileProcessing.Abstractions;
using System.IO;
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
        private readonly IMeshImporterFactory _meshImporterFactory;
        private MeshNode? _activeMeshNode;

        public MainViewModel(VisualizationEngine engine, ICameraSystem cameraSystem, IMeshImporterFactory meshImporterFactory)
        {
            Engine = engine;
            CameraSystem = cameraSystem;
            _meshImporterFactory = meshImporterFactory;

            Engine.RegisterController(new CameraKeyboardController(CameraSystem, Engine));
            Engine.RegisterController(new CameraMouseController(CameraSystem));
        }

        public VisualizationEngine Engine { get; }
        public ICameraSystem CameraSystem { get; }

        public void InitializeRendering(IRenderingContext renderingContext)
        {
            Engine.Initialize(renderingContext);
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

        public void LoadMesh(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var importer = _meshImporterFactory.GetImporter(extension);

            using var stream = File.OpenRead(filePath);
            var mesh = importer.Load(stream);

            if (_activeMeshNode is not null)
                Engine.RootNode.RemoveChild(_activeMeshNode);

            _activeMeshNode = new MeshNode { Mesh = mesh };
            Engine.RootNode.AddChild(_activeMeshNode);

            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);
            CameraSystem.ZoomToFit();
        }
    }
}
