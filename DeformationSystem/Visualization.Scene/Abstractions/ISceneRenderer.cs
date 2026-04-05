using OpenTK.Mathematics;
using Visualization.Rendering.Abstractions;
using Visualization.Scene.Nodes;

namespace Visualization.Scene.Abstractions
{
    public interface ISceneRenderer
    {
        void Render(SceneNode rootNode, IRenderingContext renderingContext, Matrix4 viewMatrix, Matrix4 projectionMatrix);
    }
}
