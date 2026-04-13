using OpenTK.Mathematics;
using Visualization.Abstractions.Geometry;
using Visualization.Rendering.Abstractions;

namespace Visualization.Scene.Nodes
{
    public sealed class MeshNode : SceneNode
    {
        private Mesh? _mesh;
        private int? _bufferId;
        private bool _isBufferDirty;

        public Mesh? Mesh
        {
            get => _mesh;
            set
            {
                if (ReferenceEquals(_mesh, value))
                    return;

                _mesh = value;
                _isBufferDirty = true;
                InvalidateBoundingBox();
            }
        }

        protected override AxisAlignedBoundingBox? LocalBoundingBox => _mesh?.LocalBoundingBox;

        public override void OnRendering(IRenderingContext renderingContext)
        {
            if (_mesh is null)
                return;

            if (_bufferId is null)
            {
                _bufferId = renderingContext.CreateBuffer(_mesh);
                _isBufferDirty = false;
            }
            else if (_isBufferDirty)
            {
                renderingContext.UpdateBuffer(_bufferId.Value, _mesh);
                _isBufferDirty = false;
            }

            renderingContext.SetMatrix("model", WorldTransform);
            renderingContext.DrawBuffer(_bufferId.Value);

            base.OnRendering(renderingContext);
        }

        public void NotifyGeometryChanged()
        {
            _mesh?.UpdateLocalBoundingBox();
            _isBufferDirty = true;
            InvalidateBoundingBox();
        }
    }
}
