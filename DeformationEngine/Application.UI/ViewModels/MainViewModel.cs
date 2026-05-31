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

        private static readonly HashSet<string> SkinningExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".gltf",
            ".glb",
            ".dae",
            ".gbd"
        };

        private readonly IMeshImporterFactory _meshImporterFactory;
        private readonly IMeshBakingService _meshBakingService;
        private readonly ILatticeVisualBuilder _latticeBuilder;
        private readonly ISkeletonVisualBuilder _skeletonBuilder;

        private MeshNode? _activeMeshNode;
        private bool _hasModel;
        private bool _hasSkinning;
        private DeformationMode _selectedMode = DeformationMode.Basic;

        #endregion

        #region Constructors

        public MainViewModel(
            ControllerEngine engine,
            ICameraSystem cameraSystem,
            IGizmoSystem gizmoSystem,
            IMeshImporterFactory meshImporterFactory,
            IMeshBakingService meshBakingService,
            ILatticeVisualBuilder latticeBuilder,
            ISkeletonVisualBuilder skeletonBuilder)
        {
            Engine = engine;
            CameraSystem = cameraSystem;
            GizmoSystem = gizmoSystem;

            _meshImporterFactory = meshImporterFactory;
            _meshBakingService = meshBakingService;
            _latticeBuilder = latticeBuilder;
            _skeletonBuilder = skeletonBuilder;

            Gizmo = new GizmoViewModel(GizmoSystem);

            Engine.RegisterController(new CameraKeyboardController(CameraSystem, Engine));

            Engine.RegisterController(new NodeSelectionController<ControlPointNode>(
                CameraSystem,
                GizmoSystem,
                () =>
                {
                    return SelectedMode == DeformationMode.Ffd;
                },
                () =>
                {
                    return _latticeBuilder.ControlPointNodes;
                },
                GizmoMode.Translate));

            Engine.RegisterController(new NodeSelectionController<BoneNode>(
                CameraSystem,
                GizmoSystem,
                () =>
                {
                    return SelectedMode == DeformationMode.LinearBlendSkinning;
                },
                () =>
                {
                    return _skeletonBuilder.BoneNodes;
                },
                GizmoMode.Rotate));

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
        public Action<string>? RequestWarning { get; set; }

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

        public bool HasSkinning
        {
            get
            {
                return _hasSkinning;
            }
            private set
            {
                if (SetProperty(ref _hasSkinning, value))
                {
                    OnPropertyChanged(nameof(AvailableModes));
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

                if (value == DeformationMode.LinearBlendSkinning && !HasSkinning)
                {
                    OnPropertyChanged();
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
                OnPropertyChanged(nameof(LbsPanelVisibility));
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

        public Visibility LbsPanelVisibility
        {
            get
            {
                return SelectedMode == DeformationMode.LinearBlendSkinning ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public IEnumerable<DeformationMode> AvailableModes
        {
            get
            {
                return Enum.GetValues<DeformationMode>().Where(mode => mode != DeformationMode.LinearBlendSkinning || HasSkinning);
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
            if (SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                _skeletonBuilder.UpdateLines();
                _activeMeshNode?.ApplyDeformers();
            }

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

            var mesh = importer.Load(filePath);

            _latticeBuilder.Clear();
            Deformers.FfdDeformer.Clear();
            _skeletonBuilder.Clear(SelectedMode == DeformationMode.Basic ? _activeMeshNode : null);

            if (_activeMeshNode is not null)
            {
                Engine.RootNode.RemoveChild(_activeMeshNode);
            }

            _activeMeshNode = new MeshNode { Mesh = mesh };

            _activeMeshNode.AddDeformer(Deformers.TwistDeformer);
            _activeMeshNode.AddDeformer(Deformers.BendDeformer);
            _activeMeshNode.AddDeformer(Deformers.FfdDeformer);
            _activeMeshNode.AddDeformer(Deformers.LbsDeformer);
            Deformers.ActiveMeshNode = _activeMeshNode;

            Engine.RootNode.AddChild(_activeMeshNode);

            Engine.RootNode.RemoveChild(GizmoSystem.GizmoNode);
            Engine.RootNode.AddChild(GizmoSystem.GizmoNode);

            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);
            CameraSystem.ZoomToFit();

            HasSkinning = mesh.Skinning?.CanSkin == true;
            WarnIfMissingSkinning(extension, HasSkinning);

            if (!HasSkinning && SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                _selectedMode = DeformationMode.Basic;
                OnPropertyChanged(nameof(SelectedMode));
            }

            HasModel = true;

            if (HasSkinning)
            {
                _skeletonBuilder.Build(_activeMeshNode, mesh, CameraSystem.TargetSphere.Radius, SelectedMode == DeformationMode.LinearBlendSkinning);
            }

            ApplySelectedModeState();
        }

        public void SetupFfdLattice()
        {
            if (_activeMeshNode?.Mesh is null)
            {
                return;
            }

            _latticeBuilder.Clear();
            Deformers.FfdDeformer.Initialize(_activeMeshNode.Mesh, Deformers.FfdResolutionX, Deformers.FfdResolutionY, Deformers.FfdResolutionZ);

            _latticeBuilder.Build(
                _activeMeshNode,
                Deformers.FfdDeformer,
                CameraSystem.TargetSphere.Radius,
                SelectedMode == DeformationMode.Ffd,
                () => { _activeMeshNode.ApplyDeformers(); }
            );

            Gizmo.Mode = GizmoMode.Translate;
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

            _latticeBuilder.Clear();
            Deformers.FfdDeformer.Clear();

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

            if (_activeMeshNode.Mesh?.Skinning is { } skinning)
            {
                skinning.Skeleton.ResetToBindPose();
                _skeletonBuilder.SyncToSkeleton();
                Deformers.IsLbsEnabled = SelectedMode == DeformationMode.LinearBlendSkinning;
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
            var bakedMesh = _meshBakingService.BakeMesh(_activeMeshNode, Deformers.TwistDeformer, Deformers.BendDeformer, Deformers.FfdDeformer);

            _activeMeshNode.Translation = Vector3.Zero;
            _activeMeshNode.Rotation = Quaternion.Identity;
            _activeMeshNode.Scale = Vector3.One;

            Deformers.TwistAngle = 0f;
            Deformers.BendAngle = 0f;

            _latticeBuilder.Clear();
            Deformers.FfdDeformer.Clear();

            if (_activeMeshNode.Mesh?.Skinning is { } updatedSkinning)
            {
                updatedSkinning.Skeleton.RebindToCurrentPose();
                _skeletonBuilder.SyncToSkeleton();
                Deformers.IsLbsEnabled = SelectedMode == DeformationMode.LinearBlendSkinning;
            }

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
            GizmoSystem.IsEnabled = _hasModel && (SelectedMode == DeformationMode.Basic || SelectedMode == DeformationMode.Ffd || SelectedMode == DeformationMode.LinearBlendSkinning);

            _latticeBuilder.SetVisibility(SelectedMode == DeformationMode.Ffd);
            _skeletonBuilder.SetVisibility(SelectedMode == DeformationMode.LinearBlendSkinning);

            Deformers.IsLbsEnabled = SelectedMode == DeformationMode.LinearBlendSkinning;

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
                _latticeBuilder.SetVisibility(true);
            }
            else if (SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                Gizmo.Mode = GizmoMode.Rotate;
                GizmoSystem.TargetNode = _skeletonBuilder.BoneNodes.FirstOrDefault();
                _skeletonBuilder.SetVisibility(true);
            }
            else
            {
                GizmoSystem.TargetNode = null;
            }
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
            _latticeBuilder.UpdateFromLattice(Deformers.FfdDeformer);
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

            if (SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                return Deformers.LbsDeformer.IsEnabled && HasSkeletonChanges();
            }

            return false;
        }

        private bool HasSkeletonChanges()
        {
            if (_activeMeshNode?.Mesh?.Skinning is not { } skinning)
            {
                return false;
            }

            return skinning.Skeleton.Bones.Any(bone => !bone.LocalTransform.IsClose(bone.BindLocalTransform));
        }

        private void WarnIfMissingSkinning(string extension, bool hasSkinning)
        {
            if (hasSkinning || !SkinningExtensions.Contains(extension))
            {
                return;
            }

            RequestWarning?.Invoke("The loaded model does not contain skeleton or skinning data. Linear Blend Skinning is unavailable for this file.");
        }

        #endregion
    }
}