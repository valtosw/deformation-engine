using OpenTK.Mathematics;
using Visualization.Rendering.Abstractions;
using Visualization.Scene.Abstractions;
using Visualization.Scene.Nodes;

namespace Visualization.Scene
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
