using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;
using Rendering.Abstractions;
using Rendering.Abstractions.Constants;

namespace Deformation.Scene
{
    public sealed class SceneRenderer : ISceneRenderer
    {
        #region Public Logic

        public void Render(SceneNode rootNode, IRenderingContext renderingContext, Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            renderingContext.BeginFrame();

            renderingContext.SetMatrix(ShaderUniforms.View, viewMatrix);
            renderingContext.SetMatrix(ShaderUniforms.Projection, projectionMatrix);

            rootNode.OnRendering(renderingContext);
        }

        #endregion
    }
}