using Deformation.Abstractions.Geometry;
using Rendering.Abstractions;

namespace Deformation.Scene.Nodes
{
    public sealed class MeshNode : SceneNode
    {
        #region Fields

        private Mesh? _mesh;
        private int? _bufferId;
        private bool _isBufferDirty;

        #endregion

        #region Properties

        public Mesh? Mesh
        {
            get => _mesh;
            set
            {
                if (ReferenceEquals(_mesh, value))
                {
                    return;
                }

                _mesh = value;
                _isBufferDirty = true;
                InvalidateBoundingBox();
            }
        }

        protected override AxisAlignedBoundingBox? LocalBoundingBox => _mesh?.LocalBoundingBox;

        #endregion

        #region Public Logic

        public override void OnRendering(IRenderingContext renderingContext)
        {
            if (_mesh is null)
            {
                return;
            }

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
            _mesh?.LocalBoundingBox = AxisAlignedBoundingBox.FromPoints(_mesh.Vertices.Select(v => v.Position));
            _isBufferDirty = true;
            InvalidateBoundingBox();
        }

        #endregion
    }
}
