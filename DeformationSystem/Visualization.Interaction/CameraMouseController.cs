using OpenTK.Mathematics;
using Visualization.Interaction.Abstractions;
using Visualization.Interaction.Input;
using Visualization.Scene.Abstractions;

namespace Visualization.Interaction
{
    public sealed class CameraMouseController(ICameraSystem cameraSystem) : IController
    {
        private Vector2 _lastMousePosition;
        private bool _isRightPressed;
        private bool _isMiddlePressed;

        public bool ProcessInput(InputEvent e)
        {
            switch (e)
            {
                case MouseClickEvent clickEvent:
                    _lastMousePosition = clickEvent.Position;

                    if (clickEvent.Button == MouseButton.Right)
                        _isRightPressed = clickEvent.InputType == InputType.Down;

                    if (clickEvent.Button == MouseButton.Middle)
                        _isMiddlePressed = clickEvent.InputType == InputType.Down;

                    return _isRightPressed || _isMiddlePressed;

                case MouseWheelEvent wheelEvent:
                    cameraSystem.Zoom(wheelEvent.Delta);
                    return true;

                case MouseMoveEvent moveEvent:
                    if (_isRightPressed)
                        cameraSystem.Orbit(_lastMousePosition, moveEvent.Position);
                    else if (_isMiddlePressed)
                        cameraSystem.Pan(_lastMousePosition, moveEvent.Position);

                    _lastMousePosition = moveEvent.Position;

                    return _isRightPressed || _isMiddlePressed;

                default:
                    return false;
            }
        }

        public void Update(float deltaTime) { }
    }
}