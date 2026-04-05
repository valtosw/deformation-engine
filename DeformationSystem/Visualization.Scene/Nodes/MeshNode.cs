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
            _isBufferDirty = true;
        }

        protected override IEnumerable<Vector3> EnumerateLocalPoints()
        {
            if (_mesh is null)
                yield break;

            foreach (var vertex in _mesh.Vertices)
                yield return vertex.Position;
        }
    }
}
