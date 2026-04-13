using Visualization.Interaction.Abstractions;
using Visualization.Interaction.Input;
using Visualization.Scene.Abstractions;
using Visualization.Scene.Enums;

namespace Visualization.Interaction
{
    public sealed class CameraKeyboardController(ICameraSystem cameraSystem) : IInputProcessor
    {
        public bool ProcessInput(IInputEvent e)
        {
            if (e is not KeyEvent { InputType: InputType.Down } keyEvent)
                return false;

            Action? action = keyEvent.Key switch
            {
                Key.F  => cameraSystem.ZoomToFit,
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

        private void ToggleCameraMode()
        {
            cameraSystem.CameraMode = cameraSystem.CameraMode == CameraMode.Perspective
                ? CameraMode.Orthographic
                : CameraMode.Perspective;
        }
    }
}