using Deformation.Abstractions.Geometry;
using Deformation.Interaction;
using Deformation.Interaction.Input;
using Deformation.IO.Abstractions;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using Rendering.Abstractions;
using System.IO;

namespace Application.UI.ViewModels
{
    public sealed class MainViewModel
    {
        #region Fields

        private readonly IMeshImporterFactory _meshImporterFactory;
        private MeshNode? _activeMeshNode;

        #endregion

        #region Constructors

        public MainViewModel(ControllerEngine engine, ICameraSystem cameraSystem, IMeshImporterFactory meshImporterFactory)
        {
            Engine = engine;
            CameraSystem = cameraSystem;
            _meshImporterFactory = meshImporterFactory;

            Engine.RegisterController(new CameraKeyboardController(CameraSystem, Engine));
            Engine.RegisterController(new CameraMouseController(CameraSystem));
        }

        #endregion

        #region Properties

        public ControllerEngine Engine { get; }
        public ICameraSystem CameraSystem { get; }

        #endregion

        #region Public Logic

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
            {
                Engine.RootNode.RemoveChild(_activeMeshNode);
            }

            _activeMeshNode = new MeshNode { Mesh = mesh };
            Engine.RootNode.AddChild(_activeMeshNode);

            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);
            CameraSystem.ZoomToFit();
        }

        #endregion
    }
}
