using Application.Core.Abstractions;
using Deformation.Abstractions.Enums;
using Deformation.Interaction;
using Deformation.Interaction.Input;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using Rendering.Abstractions;

namespace Application.Core.Services
{
    public sealed class SceneDirector : ISceneDirector
    {
        #region Fields

        private readonly ControllerEngine _engine;
        private readonly ILatticeVisualBuilder _latticeBuilder;
        private readonly ISkeletonVisualBuilder _skeletonBuilder;

        private MeshNode? _activeMeshNode;

        #endregion

        #region Constructors

        public SceneDirector(
            ControllerEngine engine,
            ICameraSystem cameraSystem,
            IGizmoSystem gizmoSystem,
            ILatticeVisualBuilder latticeBuilder,
            ISkeletonVisualBuilder skeletonBuilder)
        {
            _engine = engine;
            CameraSystem = cameraSystem;
            GizmoSystem = gizmoSystem;
            _latticeBuilder = latticeBuilder;
            _skeletonBuilder = skeletonBuilder;

            _engine.RootNode.AddChild(GizmoSystem.GizmoNode);
        }

        #endregion

        #region Properties

        public ICameraSystem CameraSystem { get; }
        public IGizmoSystem GizmoSystem { get; }
        public MeshNode? ActiveMeshNode => _activeMeshNode;

        #endregion

        #region Public Logic

        public void InitializeRendering(IRenderingContext renderingContext)
        {
            _engine.Initialize(renderingContext);
        }

        public void Resize(int width, int height)
        {
            CameraSystem.SetViewport(width, height);
        }

        public void Render(float deltaTime)
        {
            _engine.UpdateAndRender(deltaTime, CameraSystem.ViewMatrix, CameraSystem.ProjectionMatrix);
        }

        public void ProcessInput(IInputEvent inputEvent)
        {
            _engine.ProcessInput(inputEvent);
        }

        public void SetActiveMesh(MeshNode? meshNode)
        {
            if (_activeMeshNode is not null)
            {
                _engine.RootNode.RemoveChild(_activeMeshNode);
            }

            _activeMeshNode = meshNode;

            if (_activeMeshNode is not null)
            {
                _engine.RootNode.AddChild(_activeMeshNode);

                _engine.RootNode.RemoveChild(GizmoSystem.GizmoNode);
                _engine.RootNode.AddChild(GizmoSystem.GizmoNode);
            }
        }

        public void ConfigureModeVisualization(DeformationMode mode, bool hasModel)
        {
            GizmoSystem.IsEnabled = hasModel && (mode == DeformationMode.Basic || mode == DeformationMode.FreeFormDeformation || mode == DeformationMode.LinearBlendSkinning);

            _latticeBuilder.SetVisibility(mode == DeformationMode.FreeFormDeformation);
            _skeletonBuilder.SetVisibility(mode == DeformationMode.LinearBlendSkinning);

            if (!hasModel)
            {
                GizmoSystem.TargetNode = null;
                return;
            }

            if (mode == DeformationMode.Basic)
            {
                GizmoSystem.TargetNode = _activeMeshNode;
            }
            else if (mode == DeformationMode.FreeFormDeformation)
            {
                GizmoSystem.Mode = GizmoMode.Translate;
                GizmoSystem.TargetNode = null;
            }
            else if (mode == DeformationMode.LinearBlendSkinning)
            {
                GizmoSystem.Mode = GizmoMode.Rotate;
                GizmoSystem.TargetNode = _skeletonBuilder.BoneNodes.FirstOrDefault();
            }
            else
            {
                GizmoSystem.TargetNode = null;
            }
        }

        #endregion
    }
}