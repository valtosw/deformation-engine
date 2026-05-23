using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using Rendering.Abstractions;

namespace Deformation.Scene.Nodes
{
    public sealed class MeshNode : SceneNode
    {
        #region Fields

        private readonly List<IDeformer> _deformers = [];
        private Mesh? _originalMesh;
        private Mesh? _deformedMesh;

        private int? _bufferId;
        private bool _isBufferDirty;

        #endregion

        #region Properties

        public Mesh? Mesh
        {
            get
            {
                return _originalMesh;
            }
            set
            {
                if (ReferenceEquals(_originalMesh, value))
                {
                    return;
                }

                _originalMesh = value;

                if (_originalMesh is not null)
                {
                    _deformedMesh = CloneMesh(_originalMesh);
                }
                else
                {
                    _deformedMesh = null;
                }

                ApplyDeformers();
            }
        }

        protected override AxisAlignedBoundingBox? LocalBoundingBox
        {
            get
            {
                return _deformedMesh?.LocalBoundingBox;
            }
        }

        #endregion

        #region Public Logic

        public void AddDeformer(IDeformer deformer)
        {
            _deformers.Add(deformer);
            ApplyDeformers();
        }

        public void ApplyDeformers()
        {
            if (_originalMesh is null || _deformedMesh is null)
            {
                return;
            }

            ResetDeformedMeshToOriginal();

            foreach (var deformer in _deformers)
            {
                deformer.Deform(_deformedMesh);
            }

            NotifyGeometryChanged();
        }

        public void NotifyGeometryChanged()
        {
            _deformedMesh?.LocalBoundingBox = AxisAlignedBoundingBox.FromPoints(_deformedMesh.Vertices.Select(vertex => vertex.Position));

            _isBufferDirty = true;
            InvalidateBoundingBox();
        }

        public override void OnRendering(IRenderingContext renderingContext)
        {
            if (_deformedMesh is null)
            {
                return;
            }

            if (_bufferId is null)
            {
                _bufferId = renderingContext.CreateBuffer(_deformedMesh);
                _isBufferDirty = false;
            }
            else if (_isBufferDirty)
            {
                renderingContext.UpdateBuffer(_bufferId.Value, _deformedMesh);
                _isBufferDirty = false;
            }

            renderingContext.SetMatrix("model", WorldTransform);
            renderingContext.DrawBuffer(_bufferId.Value);

            base.OnRendering(renderingContext);
        }

        #endregion

        #region Private Logic

        private void ResetDeformedMeshToOriginal()
        {
            if (_originalMesh is null || _deformedMesh is null)
            {
                return;
            }

            for (var index = 0; index < _originalMesh.Vertices.Length; index++)
            {
                _deformedMesh.Vertices[index] = _originalMesh.Vertices[index];
            }
        }

        private static Mesh CloneMesh(Mesh source)
        {
            var clonedVertices = new Vertex[source.Vertices.Length];
            source.Vertices.CopyTo(clonedVertices, 0);

            var clonedIndices = new uint[source.Indices.Length];
            source.Indices.CopyTo(clonedIndices, 0);

            var clone = new Mesh(clonedVertices, clonedIndices);

            if (source.LocalBoundingBox is not null)
            {
                clone.LocalBoundingBox = new AxisAlignedBoundingBox(source.LocalBoundingBox.Min, source.LocalBoundingBox.Max);
            }

            return clone;
        }

        #endregion
    }
}