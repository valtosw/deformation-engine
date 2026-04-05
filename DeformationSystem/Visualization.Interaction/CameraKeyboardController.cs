using Visualization.Interaction.Abstractions;
using Visualization.Interaction.Input;
using Visualization.Scene.Abstractions;
using Visualization.Scene.Enums;

namespace Visualization.Interaction
{
    public sealed class CameraKeyboardController(ICameraSystem cameraSystem) : IController
    {
        public bool ProcessInput(InputEvent e)
        {
            if (e is not KeyEvent keyEvent || keyEvent.InputType != InputType.Down)
                return false;

            switch (keyEvent.Key)
            {
                case Key.F:
                    cameraSystem.ZoomToFit();
                    return true;

                case Key.V:
                    ToggleCameraMode();
                    return true;

                case Key.D1:
                    cameraSystem.SetViewPreset(ViewPreset.Front);
                    return true;

                case Key.D2:
                    cameraSystem.SetViewPreset(ViewPreset.Back);
                    return true;

                case Key.D3:
                    cameraSystem.SetViewPreset(ViewPreset.Left);
                    return true;

                case Key.D4:
                    cameraSystem.SetViewPreset(ViewPreset.Right);
                    return true;

                case Key.D5:
                    cameraSystem.SetViewPreset(ViewPreset.Top);
                    return true;

                case Key.D6:
                    cameraSystem.SetViewPreset(ViewPreset.Bottom);
                    return true;

                case Key.D7:
                    cameraSystem.SetViewPreset(ViewPreset.Isometric);
                    return true;

                default:
                    return false;
            }
        }

        public void Update(float deltaTime) { }

        private void ToggleCameraMode()
        {
            cameraSystem.CameraMode = cameraSystem.CameraMode == CameraMode.Perspective
                ? CameraMode.Orthographic
                : CameraMode.Perspective;
        }
    }
}