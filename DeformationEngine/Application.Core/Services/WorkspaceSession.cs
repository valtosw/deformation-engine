using Application.Core.Abstractions;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using Deformation.Interaction;
using Deformation.IO.Abstractions;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;

namespace Application.Core.Services
{
    public sealed class WorkspaceSession : IWorkspaceSession
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
        private readonly ISkeletonVisualBuilder _skeletonBuilder;
        private readonly ILatticeVisualBuilder _latticeBuilder;

        #endregion

        #region Constructors

        public WorkspaceSession(
            ISceneDirector sceneDirector,
            IDeformationWorkflow deformationWorkflow,
            IMeshImporterFactory meshImporterFactory,
            ISkeletonVisualBuilder skeletonBuilder,
            ILatticeVisualBuilder latticeBuilder,
            ControllerEngine engine)
        {
            Scene = sceneDirector;
            Deformations = deformationWorkflow;
            _meshImporterFactory = meshImporterFactory;
            _skeletonBuilder = skeletonBuilder;
            _latticeBuilder = latticeBuilder;

            engine.RegisterController(new CameraKeyboardController(Scene.CameraSystem, engine));

            engine.RegisterController(new NodeSelectionController<ControlPointNode>(
                Scene.CameraSystem,
                Scene.GizmoSystem,
                () => CurrentMode == DeformationMode.FreeFormDeformation,
                () => _latticeBuilder.ControlPointNodes,
                GizmoMode.Translate));

            engine.RegisterController(new NodeSelectionController<BoneNode>(
                Scene.CameraSystem,
                Scene.GizmoSystem,
                () => CurrentMode == DeformationMode.LinearBlendSkinning,
                () => _skeletonBuilder.BoneNodes,
                GizmoMode.Rotate));

            engine.RegisterController(new GizmoController(Scene.GizmoSystem, Scene.CameraSystem));
            engine.RegisterController(new CameraMouseController(Scene.CameraSystem));
        }

        #endregion

        #region Events

        public event EventHandler? StateChanged;

        #endregion

        #region Properties

        public ISceneDirector Scene { get; }
        public IDeformationWorkflow Deformations { get; }

        public bool HasModel { get; private set; }
        public bool HasSkinning { get; private set; }
        public DeformationMode CurrentMode { get; private set; }

        public Action<string>? WarningRequested { get; set; }

        #endregion

        #region Public Logic

        public void LoadMesh(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var importer = _meshImporterFactory.GetImporter(extension);
            var mesh = importer.Load(filePath);

            _latticeBuilder.Clear();
            Deformations.GetDeformer<FfdDeformer>().Clear();
            _skeletonBuilder.Clear(CurrentMode == DeformationMode.Basic ? Scene.ActiveMeshNode : null);

            var activeMeshNode = new MeshNode { Mesh = mesh };
            Deformations.AttachDeformers(activeMeshNode);
            Scene.SetActiveMesh(activeMeshNode);

            Scene.CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(activeMeshNode.BoundingBox);
            Scene.CameraSystem.ZoomToFit();

            HasSkinning = mesh.Skinning?.CanSkin == true;

            if (!HasSkinning && SkinningExtensions.Contains(extension))
            {
                WarningRequested?.Invoke("The loaded model does not contain skeleton or skinning data. Linear Blend Skinning is unavailable for this file.");
            }

            HasModel = true;

            if (HasSkinning)
            {
                _skeletonBuilder.Build(activeMeshNode, mesh, Scene.CameraSystem.TargetSphere.Radius, CurrentMode == DeformationMode.LinearBlendSkinning);
            }

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetMode(DeformationMode mode, int resolutionX, int resolutionY, int resolutionZ)
        {
            CurrentMode = mode;

            if (mode == DeformationMode.FreeFormDeformation && !Deformations.GetDeformer<FfdDeformer>().IsInitialized && Scene.ActiveMeshNode is not null)
            {
                Deformations.SetupFfdLattice(Scene.ActiveMeshNode, resolutionX, resolutionY, resolutionZ, Scene.CameraSystem.TargetSphere.Radius, true);
            }

            Scene.ConfigureModeVisualization(CurrentMode, HasModel);
            Deformations.SetLbsEnabled(CurrentMode == DeformationMode.LinearBlendSkinning, Scene.ActiveMeshNode);

            if (CurrentMode == DeformationMode.LinearBlendSkinning)
            {
                _skeletonBuilder.UpdateLines();
            }
        }

        public void SubdivideActiveMesh(int resolutionX, int resolutionY, int resolutionZ)
        {
            if (Scene.ActiveMeshNode is null)
            {
                return;
            }

            Deformations.SubdivideMesh(Scene.ActiveMeshNode, resolutionX, resolutionY, resolutionZ, Scene.CameraSystem.TargetSphere.Radius, CurrentMode);
            Scene.CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(Scene.ActiveMeshNode.BoundingBox);
            Scene.ConfigureModeVisualization(CurrentMode, HasModel);
        }

        public void BakeTransformations(int resolutionX, int resolutionY, int resolutionZ)
        {
            if (Scene.ActiveMeshNode is null)
            {
                return;
            }

            Deformations.BakeTransformations(Scene.ActiveMeshNode, resolutionX, resolutionY, resolutionZ, Scene.CameraSystem.TargetSphere.Radius, CurrentMode);
            Scene.CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(Scene.ActiveMeshNode.BoundingBox);
            Scene.ConfigureModeVisualization(CurrentMode, HasModel);
        }

        public void RestoreParameters()
        {
            if (Scene.ActiveMeshNode is null)
            {
                return;
            }

            Deformations.RestoreParameters(Scene.ActiveMeshNode, CurrentMode);
        }

        #endregion
    }
}