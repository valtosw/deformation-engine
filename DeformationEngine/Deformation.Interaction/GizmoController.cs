using Deformation.Interaction.Abstractions;
using Deformation.Interaction.Input;
using Deformation.Scene.Abstractions;

namespace Deformation.Interaction
{
    public sealed class GizmoController(IGizmoSystem gizmoSystem, ICameraSystem cameraSystem) : IInputProcessor, IUpdater
    {
        #region Public Logic

        public void Update(float deltaTime)
        {
            gizmoSystem.Update(deltaTime);
        }

        public bool ProcessInput(IInputEvent inputEvent)
        {
            if (!gizmoSystem.IsEnabled || gizmoSystem.TargetNode is null)
            {
                return false;
            }

            return inputEvent switch
            {
                MouseClickEvent mouseClickEvent => HandleClick(mouseClickEvent),
                MouseMoveEvent mouseMoveEvent => HandleMove(mouseMoveEvent),
                _ => false
            };
        }

        #endregion

        #region Private Logic

        private bool HandleClick(MouseClickEvent mouseClickEvent)
        {
            if (mouseClickEvent.Button != MouseButton.Left)
            {
                return false;
            }

            if (mouseClickEvent.InputType == InputType.Up)
            {
                return gizmoSystem.EndDrag();
            }

            var ray = cameraSystem.GetRay(mouseClickEvent.Position);

            return gizmoSystem.StartDrag(ray);
        }

        private bool HandleMove(MouseMoveEvent mouseMoveEvent)
        {
            var ray = cameraSystem.GetRay(mouseMoveEvent.Position);

            return gizmoSystem.UpdateDrag(ray);
        }

        #endregion
    }
}