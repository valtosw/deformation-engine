using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;
using Rendering.Abstractions;

namespace Deformation.Scene
{
    public sealed class SceneRenderer : ISceneRenderer
    {
        public void Render(SceneNode rootNode, IRenderingContext renderingContext, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            renderingContext.BeginFrame();

            renderingContext.SetMatrix("view", viewMatrix);
            renderingContext.SetMatrix("projection", projectionMatrix);

            rootNode.OnRendering(renderingContext);
        }
    }
}
