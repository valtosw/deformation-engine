using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Math;
using Deformation.Scene.Nodes;

namespace Deformation.Scene.Abstractions
{
    public interface IGizmoSystem
    {
        GizmoNode GizmoNode { get; }
        bool IsEnabled { get; set; }
        GizmoMode Mode { get; set; }
        SceneNode? TargetNode { get; set; }

        void Update(float deltaTime);
        bool StartDrag(Ray ray);
        bool UpdateDrag(Ray ray);
        bool EndDrag();
    }
}
