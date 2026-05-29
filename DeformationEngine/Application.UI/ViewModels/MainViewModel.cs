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
        private readonly List<ControlPointNode> _ffdControlPoints = [];

        private MeshNode? _activeMeshNode;
        private MeshNode? _latticeNode;
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
            Engine.RegisterController(new FfdSelectionController(CameraSystem, GizmoSystem, () => SelectedMode == DeformationMode.Ffd, () => _ffdControlPoints));
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
                    ApplySelectedModeState();
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
                if (_selectedMode == value)
                {
                    return;
                }

                if (HasUnbakedChanges())
                {
                    var shouldProceed = RequestConfirmation?.Invoke("You haven't baked the deformation. Switching deformation type will discard the current unbaked changes. Do you wish to proceed?");

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
                ApplySelectedModeState();

                OnPropertyChanged();
                OnPropertyChanged(nameof(BasicPanelVisibility));
                OnPropertyChanged(nameof(TwistPanelVisibility));
                OnPropertyChanged(nameof(BendPanelVisibility));
                OnPropertyChanged(nameof(FfdPanelVisibility));
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

        public Visibility FfdPanelVisibility
        {
            get
            {
                return SelectedMode == DeformationMode.Ffd ? Visibility.Visible : Visibility.Collapsed;
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

            ClearFfdState();

            if (_activeMeshNode is not null)
            {
                Engine.RootNode.RemoveChild(_activeMeshNode);
            }

            _activeMeshNode = new MeshNode { Mesh = mesh };

            _activeMeshNode.AddDeformer(Deformers.TwistDeformer);
            _activeMeshNode.AddDeformer(Deformers.BendDeformer);
            _activeMeshNode.AddDeformer(Deformers.FfdDeformer);
            Deformers.ActiveMeshNode = _activeMeshNode;

            Engine.RootNode.AddChild(_activeMeshNode);

            Engine.RootNode.RemoveChild(GizmoSystem.GizmoNode);
            Engine.RootNode.AddChild(GizmoSystem.GizmoNode);

            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);
            CameraSystem.ZoomToFit();

            HasModel = true;
            ApplySelectedModeState();
        }

        public void SetupFfdLattice()
        {
            if (_activeMeshNode?.Mesh is null)
            {
                return;
            }

            ClearFfdVisuals();

            Deformers.FfdDeformer.Initialize(_activeMeshNode.Mesh, Deformers.FfdResolutionX, Deformers.FfdResolutionY, Deformers.FfdResolutionZ);

            var lattice = Deformers.FfdDeformer.Lattice;

            if (lattice is null)
            {
                return;
            }

            Gizmo.Mode = GizmoMode.Translate;
            GizmoSystem.TargetNode = null;

            var controlPointRadius = MathF.Max(0.01f, CameraSystem.TargetSphere.Radius * 0.025f);
            var controlPointMesh = MeshFactory.CreateSphere(controlPointRadius, rings: 8, segments: 12, Vector3.Zero);

            var edgeCount =
                (lattice.ResolutionX - 1) * lattice.ResolutionY * lattice.ResolutionZ +
                lattice.ResolutionX * (lattice.ResolutionY - 1) * lattice.ResolutionZ +
                lattice.ResolutionX * lattice.ResolutionY * (lattice.ResolutionZ - 1);

            var lineVertices = new List<Vertex>(lattice.ControlPointCount);
            var lineIndices = new List<uint>(edgeCount * 2);

            for (var indexX = 0; indexX < lattice.ResolutionX; indexX++)
            {
                for (var indexY = 0; indexY < lattice.ResolutionY; indexY++)
                {
                    for (var indexZ = 0; indexZ < lattice.ResolutionZ; indexZ++)
                    {
                        var position = lattice.GetControlPoint(indexX, indexY, indexZ);

                        var controlPointNode = new ControlPointNode(indexX, indexY, indexZ, OnControlPointMoved)
                        {
                            Mesh = controlPointMesh,
                            Color = ColorConstants.ZAxisColor,
                            IsVisible = SelectedMode == DeformationMode.Ffd,
                            IgnoreDepth = false,
                            ForceSolid = true
                        };

                        controlPointNode.SetPositionFromLattice(position);

                        _ffdControlPoints.Add(controlPointNode);
                        _activeMeshNode.AddChild(controlPointNode);

                        lineVertices.Add(new Vertex(position));

                        var currentIndex = (uint)lattice.GetFlatIndex(indexX, indexY, indexZ);

                        if (indexX < lattice.ResolutionX - 1)
                        {
                            lineIndices.Add(currentIndex);
                            lineIndices.Add((uint)lattice.GetFlatIndex(indexX + 1, indexY, indexZ));
                        }

                        if (indexY < lattice.ResolutionY - 1)
                        {
                            lineIndices.Add(currentIndex);
                            lineIndices.Add((uint)lattice.GetFlatIndex(indexX, indexY + 1, indexZ));
                        }

                        if (indexZ < lattice.ResolutionZ - 1)
                        {
                            lineIndices.Add(currentIndex);
                            lineIndices.Add((uint)lattice.GetFlatIndex(indexX, indexY, indexZ + 1));
                        }
                    }
                }
            }

            var lineMesh = new Mesh([.. lineVertices], [.. lineIndices])
            {
                Topology = MeshTopology.Lines
            };

            _latticeNode = new MeshNode
            {
                Mesh = lineMesh,
                Color = new Vector3(0.6f, 0.6f, 0.6f),
                IsVisible = SelectedMode == DeformationMode.Ffd,
                IgnoreDepth = false,
                ForceWireframe = true
            };

            _activeMeshNode.AddChild(_latticeNode);
            _activeMeshNode.ApplyDeformers();
        }

        public void SubdivideActiveMesh()
        {
            if (_activeMeshNode?.Mesh is null)
            {
                return;
            }

            if (HasUnbakedChanges())
            {
                var shouldProceed = RequestConfirmation?.Invoke("Subdividing the mesh will discard the current unbaked deformation. Do you wish to proceed?");

                if (shouldProceed != true)
                {
                    return;
                }

                RestoreParameters();
            }

            var newMesh = _activeMeshNode.Mesh.Subdivide();
            ClearFfdState();

            _activeMeshNode.Mesh = newMesh;
            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);

            if (SelectedMode == DeformationMode.Ffd)
            {
                SetupFfdLattice();
            }

            ApplySelectedModeState();
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

            if (SelectedMode == DeformationMode.Ffd)
            {
                ResetFfdLattice();
            }

            _activeMeshNode.ApplyDeformers();
        }

        public void BakeTransformations()
        {
            if (_activeMeshNode is null)
            {
                return;
            }

            _activeMeshNode.ProcessPendingDeformations();

            if (_activeMeshNode.DeformedMesh is null)
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
                var transformedNormal = vertex.Normal.LengthSquared > MathConstants.LengthTolerance
                    ? normalMatrix.TransformDirection(vertex.Normal).Normalized()
                    : vertex.Normal;

                newVertices[index] = new Vertex(transformedPosition, transformedNormal, vertex.TexCoords);
            }

            var newIndices = new uint[currentDeformed.Indices.Length];
            currentDeformed.Indices.CopyTo(newIndices, 0);

            var bakedMesh = new Mesh(newVertices, newIndices)
            {
                Topology = currentDeformed.Topology
            };

            _activeMeshNode.Translation = Vector3.Zero;
            _activeMeshNode.Rotation = Quaternion.Identity;
            _activeMeshNode.Scale = Vector3.One;

            Deformers.TwistAngle = 0f;
            Deformers.BendAngle = 0f;
            ClearFfdState();

            _activeMeshNode.Mesh = bakedMesh;
            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);

            if (SelectedMode == DeformationMode.Ffd)
            {
                SetupFfdLattice();
            }

            ApplySelectedModeState();
        }

        #endregion

        #region Private Logic

        private void ApplySelectedModeState()
        {
            GizmoSystem.IsEnabled = _hasModel && (SelectedMode == DeformationMode.Basic || SelectedMode == DeformationMode.Ffd);
            SetFfdVisualsVisible(SelectedMode == DeformationMode.Ffd);

            if (!_hasModel)
            {
                GizmoSystem.TargetNode = null;
                return;
            }

            if (SelectedMode == DeformationMode.Basic)
            {
                GizmoSystem.TargetNode = _activeMeshNode;
            }
            else if (SelectedMode == DeformationMode.Ffd)
            {
                if (!Deformers.FfdDeformer.IsInitialized)
                {
                    SetupFfdLattice();
                }

                Gizmo.Mode = GizmoMode.Translate;
                GizmoSystem.TargetNode = null;
                SetFfdVisualsVisible(true);
            }
            else
            {
                GizmoSystem.TargetNode = null;
            }
        }

        private void OnControlPointMoved(int indexX, int indexY, int indexZ, Vector3 newPosition)
        {
            var lattice = Deformers.FfdDeformer.Lattice;

            if (lattice is null)
            {
                return;
            }

            Deformers.FfdDeformer.UpdateControlPoint(indexX, indexY, indexZ, newPosition);

            if (_latticeNode?.Mesh is not null)
            {
                var flatIndex = lattice.GetFlatIndex(indexX, indexY, indexZ);
                _latticeNode.Mesh.Vertices[flatIndex].Position = newPosition;
                _latticeNode.ApplyDeformers();
            }

            _activeMeshNode?.ApplyDeformers();
        }

        private void ResetFfdLattice()
        {
            if (!Deformers.FfdDeformer.IsInitialized)
            {
                if (SelectedMode == DeformationMode.Ffd)
                {
                    SetupFfdLattice();
                }

                return;
            }

            Deformers.FfdDeformer.Reset();
            UpdateFfdVisualsFromLattice();
        }

        private void UpdateFfdVisualsFromLattice()
        {
            var lattice = Deformers.FfdDeformer.Lattice;

            if (lattice is null)
            {
                return;
            }

            foreach (var controlPointNode in _ffdControlPoints)
            {
                var position = lattice.GetControlPoint(controlPointNode.IndexX, controlPointNode.IndexY, controlPointNode.IndexZ);
                controlPointNode.SetPositionFromLattice(position);
            }

            if (_latticeNode?.Mesh is null)
            {
                return;
            }

            for (var indexX = 0; indexX < lattice.ResolutionX; indexX++)
            {
                for (var indexY = 0; indexY < lattice.ResolutionY; indexY++)
                {
                    for (var indexZ = 0; indexZ < lattice.ResolutionZ; indexZ++)
                    {
                        var flatIndex = lattice.GetFlatIndex(indexX, indexY, indexZ);
                        _latticeNode.Mesh.Vertices[flatIndex].Position = lattice.GetControlPoint(indexX, indexY, indexZ);
                    }
                }
            }

            _latticeNode.ApplyDeformers();
        }

        private void SetFfdVisualsVisible(bool isVisible)
        {
            foreach (var controlPointNode in _ffdControlPoints)
            {
                controlPointNode.IsVisible = isVisible;
            }

            if (_latticeNode is not null)
            {
                _latticeNode.IsVisible = isVisible;
            }

            if (!isVisible && GizmoSystem.TargetNode is ControlPointNode)
            {
                GizmoSystem.TargetNode = null;
            }
        }

        private void ClearFfdState()
        {
            ClearFfdVisuals();
            Deformers.FfdDeformer.Clear();
        }

        private void ClearFfdVisuals()
        {
            foreach (var controlPointNode in _ffdControlPoints)
            {
                controlPointNode.Parent?.RemoveChild(controlPointNode);
            }

            _ffdControlPoints.Clear();

            if (_latticeNode is not null)
            {
                _latticeNode.Parent?.RemoveChild(_latticeNode);
                _latticeNode = null;
            }

            if (GizmoSystem.TargetNode is ControlPointNode)
            {
                GizmoSystem.TargetNode = null;
            }
        }

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

            if (SelectedMode == DeformationMode.Ffd)
            {
                return Deformers.FfdDeformer.HasChanges;
            }

            return false;
        }

        #endregion
    }
}
