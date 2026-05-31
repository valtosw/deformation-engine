using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Abstractions;
using OpenTK.Mathematics;
using Rendering.Abstractions;
using Rendering.Abstractions.Constants;

namespace Deformation.Scene.Nodes
{
    public class MeshNode : SceneNode
    {
        #region Fields

        private readonly List<IDeformer> _deformers = [];
        private Mesh? _originalMesh;
        private Mesh? _deformedMesh;

        private int? _bufferId;
        private bool _isBufferDirty;
        private bool _isDeformationDirty;

        #endregion

        #region Properties

        public Vector3 Color { get; set; } = ColorConstants.DefaultObjectColor;

        public bool IgnoreDepth { get; set; }
        public bool ForceSolid { get; set; }
        public bool ForceWireframe { get; set; }

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
                _deformedMesh = _originalMesh is not null ? CloneMesh(_originalMesh) : null;

                ApplyDeformers();
            }
        }

        public Mesh? DeformedMesh
        {
            get
            {
                return _deformedMesh;
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
            _isDeformationDirty = true;
        }

        public void ProcessPendingDeformations()
        {
            if (!_isDeformationDirty)
            {
                return;
            }

            ProcessDeformations();
            _isDeformationDirty = false;
        }

        public void NotifyGeometryChanged()
        {
            _deformedMesh?.LocalBoundingBox = AxisAlignedBoundingBox.FromPoints(_deformedMesh.Vertices.Select(vertex => vertex.Position));

            _isBufferDirty = true;
            InvalidateBoundingBox();
        }

        public override void OnRendering(IRenderingContext renderingContext)
        {
            ProcessPendingDeformations();

            if (!IsVisible)
            {
                return;
            }

            if (_deformedMesh is null)
            {
                base.OnRendering(renderingContext);
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

            if (ForceSolid)
            {
                renderingContext.SetWireframeOverride(false);
            }
            else if (ForceWireframe)
            {
                renderingContext.SetWireframeOverride(true);
            }

            renderingContext.SetVector(ShaderUniforms.ObjectColor, Color);
            renderingContext.SetMatrix(ShaderUniforms.Model, WorldTransform);
            renderingContext.DrawBuffer(_bufferId.Value, IgnoreDepth);

            if (ForceSolid || ForceWireframe)
            {
                renderingContext.SetWireframeOverride(null);
            }

            base.OnRendering(renderingContext);
        }

        #endregion

        #region Private Logic

        private void ProcessDeformations()
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

            var clone = new Mesh(clonedVertices, clonedIndices)
            {
                Topology = source.Topology,
                Skinning = source.Skinning
            };

            if (source.LocalBoundingBox is not null)
            {
                clone.LocalBoundingBox = new AxisAlignedBoundingBox(source.LocalBoundingBox.Min, source.LocalBoundingBox.Max);
            }

            return clone;
        }

        #endregion
    }
}