using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Interaction.Abstractions;
using Deformation.Interaction.Input;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;

namespace Deformation.Interaction
{
    public sealed class FfdSelectionController(
        ICameraSystem cameraSystem,
        IGizmoSystem gizmoSystem,
        Func<bool> isEnabled,
        Func<IEnumerable<ControlPointNode>> controlPointProvider)
        : IInputProcessor
    {
        #region Public Logic

        public bool ProcessInput(IInputEvent inputEvent)
        {
            if (!isEnabled())
            {
                return false;
            }

            if (inputEvent is not MouseClickEvent mouseClickEvent)
            {
                return false;
            }

            if (mouseClickEvent.Button != MouseButton.Left || mouseClickEvent.InputType != InputType.Down)
            {
                return false;
            }

            var ray = cameraSystem.GetRay(mouseClickEvent.Position);
            ControlPointNode? closestNode = null;
            var minimumDistance = float.MaxValue;

            foreach (var controlPointNode in controlPointProvider())
            {
                if (!controlPointNode.IsVisible || controlPointNode.Mesh?.LocalBoundingBox is null)
                {
                    continue;
                }

                var inverseTransform = controlPointNode.WorldTransform.Inverted();
                var localRay = ray.Transformed(inverseTransform);

                if (localRay.Intersects(controlPointNode.Mesh.LocalBoundingBox, out var distance))
                {
                    if (distance < minimumDistance)
                    {
                        minimumDistance = distance;
                        closestNode = controlPointNode;
                    }
                }
            }

            if (closestNode is not null)
            {
                gizmoSystem.Mode = GizmoMode.Translate;
                gizmoSystem.TargetNode = closestNode;
                return true;
            }

            return false;
        }

        #endregion
    }
}
