using OpenTK.Mathematics;
using Visualization.Interaction.Abstractions;
using Visualization.Interaction.Input;
using Visualization.Rendering.Abstractions;
using Visualization.Scene.Abstractions;
using Visualization.Scene.Nodes;

namespace Visualization.Interaction
{
    public sealed class VisualizationEngine(ISceneRenderer renderer)
    {
        private readonly List<IController> _controllers = [];

        public SceneNode RootNode { get; } = new();
        public IRenderingContext? RenderingContext { get; private set; }

        public void RegisterController(IController controller)
        {
            _controllers.Add(controller);
        }

        public void Initialize(IRenderingContext context)
        {
            RenderingContext = context;
        }

        public void ProcessInput(InputEvent e)
        {
            for (var i = _controllers.Count - 1; i >= 0; i--)
            {
                if (_controllers[i].ProcessInput(e))
                    break;
            }
        }

        public void UpdateAndRender(float deltaTime, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            if (RenderingContext is null) 
                return;

            foreach (var controller in _controllers)
                controller.Update(deltaTime);

            renderer.Render(RootNode, RenderingContext, viewMatrix, projectionMatrix);
        }
    }
}
