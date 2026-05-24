using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
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
using System.Windows;

namespace Application.UI.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        #region Fields

        private readonly IMeshImporterFactory _meshImporterFactory;
        private MeshNode? _activeMeshNode;
        private bool _hasModel;
        private DeformationMode _selectedMode = DeformationMode.Basic;

        #endregion

        #region Constructors

        public MainViewModel(ControllerEngine engine, ICameraSystem cameraSystem, IGizmoSystem gizmoSystem, IMeshImporterFactory meshImporterFactory)
        {
            Engine = engine;
            CameraSystem = cameraSystem;
            GizmoSystem = gizmoSystem;
            _meshImporterFactory = meshImporterFactory;

            Gizmo = new GizmoViewModel(GizmoSystem);

            Engine.RegisterController(new CameraKeyboardController(CameraSystem, Engine));
            Engine.RegisterController(new GizmoController(GizmoSystem, CameraSystem));
            Engine.RegisterController(new CameraMouseController(CameraSystem));

            Engine.RootNode.AddChild(GizmoSystem.GizmoNode);
        }

        #endregion

        #region Properties

        public ControllerEngine Engine { get; }
        public ICameraSystem CameraSystem { get; }
        public IGizmoSystem GizmoSystem { get; }

        public DeformerViewModel Deformers { get; } = new();
        public GizmoViewModel Gizmo { get; }

        public Func<string, bool>? RequestConfirmation { get; set; }

        public bool HasModel
        {
            get
            {
                return _hasModel;
            }
            private set
            {
                if (SetProperty(ref _hasModel, value))
                {
                    GizmoSystem.IsEnabled = value && SelectedMode == DeformationMode.Basic;
                }
            }
        }

        public DeformationMode SelectedMode
        {
            get
            {
                return _selectedMode;
            }
            set
            {
                if (_selectedMode != value)
                {
                    if (HasUnbakedChanges())
                    {
                        var shouldProceed = RequestConfirmation?.Invoke("You haven't baked the deformation. The changes won't be applied when you switch the deformation type. Do you wish to proceed?");

                        if (shouldProceed != true)
                        {
                            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                OnPropertyChanged(nameof(SelectedMode));
                            }));

                            return;
                        }

                        RestoreParameters();
                    }

                    _selectedMode = value;
                    GizmoSystem.IsEnabled = _hasModel && _selectedMode == DeformationMode.Basic;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BasicPanelVisibility));
                    OnPropertyChanged(nameof(TwistPanelVisibility));
                    OnPropertyChanged(nameof(BendPanelVisibility));
                }
            }
        }

        public Visibility BasicPanelVisibility
        {
            get
            {
                return SelectedMode == DeformationMode.Basic ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility TwistPanelVisibility
        {
            get
            {
                return SelectedMode == DeformationMode.Twist ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility BendPanelVisibility
        {
            get
            {
                return SelectedMode == DeformationMode.Bend ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public static IEnumerable<DeformationMode> AvailableModes
        {
            get
            {
                return Enum.GetValues<DeformationMode>();
            }
        }

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

            GizmoSystem.TargetNode = _activeMeshNode;

            Engine.RootNode.AddChild(_activeMeshNode);

            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);
            CameraSystem.ZoomToFit();

            HasModel = true;
        }

        public void RestoreParameters()
        {
            if (_activeMeshNode is null)
            {
                return;
            }

            _activeMeshNode.Translation = Vector3.Zero;
            _activeMeshNode.Rotation = Quaternion.Identity;
            _activeMeshNode.Scale = Vector3.One;

            Deformers.TwistAngle = 0f;
            Deformers.BendAngle = 0f;
            Deformers.TwistPivot = 0.5f;
            Deformers.BendPivot = 0.5f;
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

            Deformers.TwistAngle = 0f;
            Deformers.BendAngle = 0f;

            _activeMeshNode.Mesh = bakedMesh;

            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);
        }

        #endregion

        #region Private Logic

        private bool HasUnbakedChanges()
        {
            if (_activeMeshNode is null)
            {
                return false;
            }

            if (SelectedMode == DeformationMode.Basic)
            {
                return _activeMeshNode.Translation != Vector3.Zero ||
                       _activeMeshNode.Rotation != Quaternion.Identity ||
                       _activeMeshNode.Scale != Vector3.One;
            }

            if (SelectedMode == DeformationMode.Twist)
            {
                return MathF.Abs(Deformers.TwistAngle) > MathConstants.ZeroTolerance;
            }

            if (SelectedMode == DeformationMode.Bend)
            {
                return MathF.Abs(Deformers.BendAngle) > MathConstants.ZeroTolerance;
            }

            return false;
        }

        #endregion
    }
}