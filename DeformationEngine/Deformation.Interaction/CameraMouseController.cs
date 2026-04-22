using Deformation.Interaction.Abstractions;
using Deformation.Interaction.Input;
using Deformation.Scene.Abstractions;
using OpenTK.Mathematics;

namespace Deformation.Interaction
{
    public sealed class CameraMouseController(ICameraSystem cameraSystem) : IInputProcessor
    {
        #region Fields

        private Vector2 _lastMousePosition;
        private bool _isRightPressed;
        private bool _isMiddlePressed;

        #endregion

        #region Public Logic

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

        #endregion

        #region Private Logic

        private bool HandleClick(MouseClickEvent mouseClickEvent)
        {
            _lastMousePosition = mouseClickEvent.Position;

            if (mouseClickEvent.Button == MouseButton.Right)
            {
                _isRightPressed = mouseClickEvent.InputType == InputType.Down;
            }

            if (mouseClickEvent.Button == MouseButton.Middle)
            {
                _isMiddlePressed = mouseClickEvent.InputType == InputType.Down;
            }

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
            {
                cameraSystem.Orbit(_lastMousePosition, mouseMoveEvent.Position);
            }
            else if (_isMiddlePressed)
            {
                cameraSystem.Pan(_lastMousePosition, mouseMoveEvent.Position);
            }

            _lastMousePosition = mouseMoveEvent.Position;

            return _isRightPressed || _isMiddlePressed;
        }

        #endregion
    }
}