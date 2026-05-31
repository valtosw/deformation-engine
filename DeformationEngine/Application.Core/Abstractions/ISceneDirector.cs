using Deformation.Abstractions.Enums;
using Deformation.Interaction.Input;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using Rendering.Abstractions;

namespace Application.Core.Abstractions
{
    public interface ISceneDirector
    {
        ICameraSystem CameraSystem { get; }
        IGizmoSystem GizmoSystem { get; }
        MeshNode? ActiveMeshNode { get; }

        void InitializeRendering(IRenderingContext renderingContext);
        void Resize(int width, int height);
        void Render(float deltaTime);
        void ProcessInput(IInputEvent inputEvent);

        void SetActiveMesh(MeshNode? meshNode);
        void ConfigureModeVisualization(DeformationMode mode, bool hasModel);
    }
}