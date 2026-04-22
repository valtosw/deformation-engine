using Deformation.Interaction.Abstractions;
using Deformation.Interaction.Input;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;
using Rendering.Abstractions;

namespace Deformation.Interaction
{
    public sealed class ControllerEngine(ISceneRenderer renderer)
    {
        #region Fields

        private readonly List<IController> _controllers = [];
        private readonly List<IInputProcessor> _inputProcessors = [];
        private readonly List<IUpdater> _updaters = [];

        #endregion

        #region Properties

        public SceneNode RootNode { get; } = new();
        public IRenderingContext? RenderingContext { get; private set; }

        #endregion

        #region Public Logic

        public void RegisterController(IController controller)
        {
            _controllers.Add(controller);

            if (controller is IInputProcessor inputProcessor)
            {
                _inputProcessors.Add(inputProcessor);
            }

            if (controller is IUpdater updater)
            {
                _updaters.Add(updater);
            }
        }

        public void Initialize(IRenderingContext context)
        {
            RenderingContext = context;
        }

        public void ProcessInput(IInputEvent e)
        {
            for (var i = _inputProcessors.Count - 1; i >= 0; i--)
            {
                if (_inputProcessors[i].ProcessInput(e))
                {
                    break;
                }
            }
        }

        public void UpdateAndRender(float deltaTime, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            if (RenderingContext is null)
            {
                return;
            }

            foreach (var updater in _updaters)
            {
                updater.Update(deltaTime);
            }

            renderer.Render(RootNode, RenderingContext, viewMatrix, projectionMatrix);
        }

        #endregion
    }
}
