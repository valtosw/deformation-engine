using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Interaction.Abstractions;
using Deformation.Interaction.Input;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;

namespace Deformation.Interaction
{
    public sealed class LbsSelectionController(
        ICameraSystem cameraSystem,
        IGizmoSystem gizmoSystem,
        Func<bool> isEnabled,
        Func<IEnumerable<BoneNode>> boneProvider)
        : IInputProcessor
    {
        public bool ProcessInput(IInputEvent inputEvent)
        {
            if (!isEnabled() || inputEvent is not MouseClickEvent mouseClickEvent)
            {
                return false;
            }

            if (mouseClickEvent.Button != MouseButton.Left || mouseClickEvent.InputType != InputType.Down)
            {
                return false;
            }

            var ray = cameraSystem.GetRay(mouseClickEvent.Position);
            BoneNode? closestNode = null;
            var minimumDistance = float.MaxValue;

            foreach (var boneNode in boneProvider())
            {
                if (!boneNode.IsVisible || boneNode.Mesh?.LocalBoundingBox is null)
                {
                    continue;
                }

                var localRay = ray.Transformed(boneNode.WorldTransform.Inverted());

                if (localRay.Intersects(boneNode.Mesh.LocalBoundingBox, out var distance) && distance < minimumDistance)
                {
                    minimumDistance = distance;
                    closestNode = boneNode;
                }
            }

            if (closestNode is null)
            {
                return false;
            }

            gizmoSystem.Mode = GizmoMode.Rotate;
            gizmoSystem.TargetNode = closestNode;
            return true;
        }
    }
}
