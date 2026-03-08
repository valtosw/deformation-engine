using OpenTK.Mathematics;
using Visualization.Interaction.Input;
using Visualization.Rendering.Abstractions;
using Visualization.Scene;
using Visualization.Scene.Nodes;

namespace Visualization.Interaction
{
    public sealed class SceneController
    {
        private readonly SceneNode _rootNode = new();
        private readonly SceneRenderer _sceneRenderer = new();
        private readonly CameraSystem _cameraSystem = new();

        private IRenderingContext? _renderingContext;
        private Vector2 _lastMousePosition;

        public SceneNode RootNode => _rootNode;

        public void Initialize(IRenderingContext renderingContext) => _renderingContext = renderingContext;

        public void OnViewportResize(int width, int height) => _cameraSystem.SetViewport(width, height);

        public void ProcessInput(InputEvent e, bool isRightPressed, bool isMiddlePressed)
        {
            switch (e)
            {
                case MouseWheelEvent wheelEvent:
                    _cameraSystem.Zoom(wheelEvent.Delta);
                    break;

                case MouseMoveEvent moveEvent when isRightPressed:
                    var delta = moveEvent.Position - _lastMousePosition;
                    _cameraSystem.Orbit(delta * 0.005f);
                    _lastMousePosition = moveEvent.Position;
                    break;

                case MouseMoveEvent moveEvent when isMiddlePressed:
                    var panDelta = moveEvent.Position - _lastMousePosition;
                    _cameraSystem.Pan(panDelta * 0.01f);
                    _lastMousePosition = moveEvent.Position;
                    break;

                case MouseMoveEvent moveEvent:
                    _lastMousePosition = moveEvent.Position;
                    break;
            }
        }

        public void Render(float deltaTime)
        {
            if (_renderingContext is null)
                return;

            _sceneRenderer.Render(_rootNode, _renderingContext, _cameraSystem.ViewMatrix, _cameraSystem.ProjectionMatrix);
        }

        public void Orbit(Vector2 delta) => _cameraSystem.Orbit(delta);
        public void Pan(Vector2 delta) => _cameraSystem.Pan(delta);
        public void Zoom(float delta) => _cameraSystem.Zoom(delta);
    }
}
