using Deformation.Interaction.Abstractions;
using Deformation.Interaction.Input;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Enums;

namespace Deformation.Interaction
{
    public sealed class CameraKeyboardController(ICameraSystem cameraSystem, ControllerEngine engine) : IInputProcessor
    {
        #region Public Logic

        public bool ProcessInput(IInputEvent e)
        {
            if (e is not KeyEvent { InputType: InputType.Down } keyEvent)
            {
                return false;
            }

            Action? action = keyEvent.Key switch
            {
                Key.F  => cameraSystem.ZoomToFit,
                Key.P  => ToggleWireframeMode,
                Key.V  => ToggleCameraMode,
                Key.D1 => () => cameraSystem.SetViewPreset(ViewPreset.Front),
                Key.D2 => () => cameraSystem.SetViewPreset(ViewPreset.Back),
                Key.D3 => () => cameraSystem.SetViewPreset(ViewPreset.Left),
                Key.D4 => () => cameraSystem.SetViewPreset(ViewPreset.Right),
                Key.D5 => () => cameraSystem.SetViewPreset(ViewPreset.Top),
                Key.D6 => () => cameraSystem.SetViewPreset(ViewPreset.Bottom),
                Key.D7 => () => cameraSystem.SetViewPreset(ViewPreset.Isometric),
                _      => null
            };

            action?.Invoke();
            return action is not null;
        }

        #endregion

        #region Private Logic

        private void ToggleCameraMode()
        {
            cameraSystem.CameraMode = cameraSystem.CameraMode == CameraMode.Perspective
                ? CameraMode.Orthographic
                : CameraMode.Perspective;
        }

        private void ToggleWireframeMode()
        {
            if (engine.RenderingContext is null)
            {
                return;
            }

            engine.RenderingContext.IsWireframeEnabled = !engine.RenderingContext.IsWireframeEnabled;
        }

        #endregion
    }
}