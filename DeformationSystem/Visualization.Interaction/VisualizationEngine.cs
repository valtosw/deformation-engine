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
        private readonly List<IInputProcessor> _inputProcessors = [];
        private readonly List<IUpdater> _updaters = [];

        public SceneNode RootNode { get; } = new();
        public IRenderingContext? RenderingContext { get; private set; }

        public void RegisterController(IController controller)
        {
            _controllers.Add(controller);

            if (controller is IInputProcessor inputProcessor)
                _inputProcessors.Add(inputProcessor);

            if (controller is IUpdater updater)
                _updaters.Add(updater);
        }

        public void Initialize(IRenderingContext context)
        {
            RenderingContext = context;
        }

        public void ProcessInput(InputEvent e)
        {
            for (var i = _inputProcessors.Count - 1; i >= 0; i--)
            {
                if (_inputProcessors[i].ProcessInput(e))
                    break;
            }
        }

        public void UpdateAndRender(float deltaTime, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            if (RenderingContext is null) 
                return;

            foreach (var updater in _updaters)
                updater.Update(deltaTime);

            renderer.Render(RootNode, RenderingContext, viewMatrix, projectionMatrix);
        }
    }
}
