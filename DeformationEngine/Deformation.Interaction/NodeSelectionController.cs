using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Interaction.Abstractions;
using Deformation.Interaction.Input;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;

namespace Deformation.Interaction
{
    public sealed class NodeSelectionController<TNode>(
        ICameraSystem cameraSystem,
        IGizmoSystem gizmoSystem,
        Func<bool> isEnabled,
        Func<IEnumerable<TNode>> nodeProvider,
        GizmoMode gizmoMode) : IInputProcessor where TNode : MeshNode
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
            TNode? closestNode = null;
            var minimumDistance = float.MaxValue;

            foreach (var node in nodeProvider())
            {
                if (!node.IsVisible || node.Mesh?.LocalBoundingBox is null)
                {
                    continue;
                }

                var inverseTransform = node.WorldTransform.Inverted();
                var localRay = ray.Transformed(inverseTransform);

                if (localRay.Intersects(node.Mesh.LocalBoundingBox, out var distance))
                {
                    if (distance < minimumDistance)
                    {
                        minimumDistance = distance;
                        closestNode = node;
                    }
                }
            }

            if (closestNode is not null)
            {
                gizmoSystem.Mode = gizmoMode;
                gizmoSystem.TargetNode = closestNode;

                return true;
            }

            return false;
        }

        #endregion
    }
}