using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Interaction;
using Deformation.Interaction.Input;
using Deformation.IO.Abstractions;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;
using Rendering.Abstractions;
using System.IO;

namespace Application.UI.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        #region Fields

        private readonly IMeshImporterFactory _meshImporterFactory;
        private readonly GizmoController _gizmoController;
        private MeshNode? _activeMeshNode;

        #endregion

        #region Constructors

        public MainViewModel(ControllerEngine engine, ICameraSystem cameraSystem, IMeshImporterFactory meshImporterFactory)
        {
            Engine = engine;
            CameraSystem = cameraSystem;
            _meshImporterFactory = meshImporterFactory;

            _gizmoController = new GizmoController(CameraSystem);
            Gizmo = new GizmoViewModel(_gizmoController);

            Engine.RegisterController(new CameraKeyboardController(CameraSystem, Engine));
            Engine.RegisterController(_gizmoController);
            Engine.RegisterController(new CameraMouseController(CameraSystem));

            Engine.RootNode.AddChild(_gizmoController.GizmoNode);
        }

        #endregion

        #region Properties

        public ControllerEngine Engine { get; }
        public ICameraSystem CameraSystem { get; }
        public DeformerViewModel Deformers { get; } = new();
        public GizmoViewModel Gizmo { get; }

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

            _activeMeshNode.AddDeformer(Deformers.TwistDeformer);
            _activeMeshNode.AddDeformer(Deformers.BendDeformer);
            Deformers.ActiveMeshNode = _activeMeshNode;

            _gizmoController.TargetNode = _activeMeshNode;

            Engine.RootNode.AddChild(_activeMeshNode);

            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);
            CameraSystem.ZoomToFit();
        }

        public void BakeTransformations()
        {
            if (_activeMeshNode is null || _activeMeshNode.DeformedMesh is null)
            {
                return;
            }

            var currentDeformed = _activeMeshNode.DeformedMesh;
            var worldMatrix = _activeMeshNode.LocalTransform;
            var normalMatrix = new Matrix4(worldMatrix.Row0, worldMatrix.Row1, worldMatrix.Row2, worldMatrix.Row3);

            normalMatrix.Invert();
            normalMatrix.Transpose();

            var newVertices = new Vertex[currentDeformed.Vertices.Length];

            for (var index = 0; index < currentDeformed.Vertices.Length; index++)
            {
                var vertex = currentDeformed.Vertices[index];

                var transformedPosition = worldMatrix.TransformPoint(vertex.Position);
                var transformedNormal = normalMatrix.TransformDirection(vertex.Normal).Normalized();

                newVertices[index] = new Vertex(transformedPosition, transformedNormal, vertex.TexCoords);
            }

            var newIndices = new uint[currentDeformed.Indices.Length];
            currentDeformed.Indices.CopyTo(newIndices, 0);

            var bakedMesh = new Mesh(newVertices, newIndices);

            _activeMeshNode.Translation = Vector3.Zero;
            _activeMeshNode.Rotation = Quaternion.Identity;
            _activeMeshNode.Scale = Vector3.One;

            Deformers.TwistAngle = 0;
            Deformers.BendAngle = 0;

            _activeMeshNode.Mesh = bakedMesh;

            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);
        }

        #endregion
    }
}