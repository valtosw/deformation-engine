using OpenTK.Mathematics;
using Visualization.Interaction.Abstractions;
using Visualization.Interaction.Input;
using Visualization.Scene.Abstractions;

namespace Visualization.Interaction
{
    public sealed class CameraMouseController(ICameraSystem cameraSystem) : IInputProcessor
    {
        private Vector2 _lastMousePosition;
        private bool _isRightPressed;
        private bool _isMiddlePressed;

        public bool ProcessInput(IInputEvent e)
        {
            return e switch
            {
                MouseClickEvent mouseClickEvent => HandleClick(mouseClickEvent),
                MouseWheelEvent mouseWheelEvent => HandleWheel(mouseWheelEvent),
                MouseMoveEvent  mouseMoveEvent  => HandleMove(mouseMoveEvent),
                _ => false
            };
        }

        private bool HandleClick(MouseClickEvent mouseClickEvent)
        {
            _lastMousePosition = mouseClickEvent.Position;

            if (mouseClickEvent.Button == MouseButton.Right)
                _isRightPressed = mouseClickEvent.InputType == InputType.Down;

            if (mouseClickEvent.Button == MouseButton.Middle)
                _isMiddlePressed = mouseClickEvent.InputType == InputType.Down;

            return _isRightPressed || _isMiddlePressed;
        }

        private bool HandleWheel(MouseWheelEvent mouseWheelEvent)
        {
            cameraSystem.Zoom(mouseWheelEvent.Delta);
            return true;
        }

        private bool HandleMove(MouseMoveEvent mouseMoveEvent)
        {
            if (_isRightPressed)
                cameraSystem.Orbit(_lastMousePosition, mouseMoveEvent.Position);
            else if (_isMiddlePressed)
                cameraSystem.Pan(_lastMousePosition, mouseMoveEvent.Position);

            _lastMousePosition = mouseMoveEvent.Position;

            return _isRightPressed || _isMiddlePressed;
        }
    }
}