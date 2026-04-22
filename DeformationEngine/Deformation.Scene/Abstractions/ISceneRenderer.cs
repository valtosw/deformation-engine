using Deformation.Scene.Nodes;
using OpenTK.Mathematics;
using Rendering.Abstractions;

namespace Deformation.Scene.Abstractions
{
    public interface ISceneRenderer
    {
        void Render(SceneNode rootNode, IRenderingContext renderingContext, Matrix4 viewMatrix, Matrix4 projectionMatrix);
    }
}
