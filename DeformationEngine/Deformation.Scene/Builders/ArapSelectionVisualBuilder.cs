using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;

namespace Deformation.Scene.Builders
{
    public sealed class ArapSelectionVisualBuilder(IGizmoSystem gizmoSystem) : IArapSelectionVisualBuilder
    {
        #region Fields

        private ArapDeformer? _deformer;
        private MeshNode? _parentNode;
        private MeshNode? _controlVisualNode;
        private MeshNode? _anchorVisualNode;
        private float _markerRadius;
        private bool _isVisible;

        #endregion

        #region Properties

        public ArapHandleNode? HandleNode { get; private set; }

        #endregion

        #region Public Logic

        public void Build(MeshNode parentNode, ArapDeformer deformer, float targetSphereRadius, bool isVisible, Action onHandleChanged)
        {
            Clear();

            _parentNode = parentNode;
            _deformer = deformer;
            _markerRadius = MathF.Max(0.01f, targetSphereRadius * 0.0125f);
            _isVisible = isVisible;

            _controlVisualNode = new MeshNode
            {
                Color = ColorConstants.ArapControlPointColor,
                ForceSolid = true,
                IgnoreDepth = false,
                IsVisible = isVisible
            };

            _anchorVisualNode = new MeshNode
            {
                Color = ColorConstants.ArapAnchorPointColor,
                ForceSolid = true,
                IgnoreDepth = false,
                IsVisible = isVisible
            };

            HandleNode = new ArapHandleNode((position, rotation) =>
            {
                deformer.SetHandleTransform(position, rotation);
                onHandleChanged();
            })
            {
                Mesh = MeshFactory.CreateSphere(_markerRadius * 1.5f, rings: 6, segments: 8, Vector3.Zero),
                Color = ColorConstants.ZAxisColor,
                ForceSolid = true,
                IgnoreDepth = false,
                IsVisible = false
            };

            HandleNode.SetPose(deformer.Pivot, Quaternion.Identity);

            parentNode.AddChild(_controlVisualNode);
            parentNode.AddChild(_anchorVisualNode);
            parentNode.AddChild(HandleNode);

            deformer.SelectionChanged += OnSelectionChanged;

            Refresh();
        }

        public void Refresh()
        {
            if (_deformer is null || _controlVisualNode is null || _anchorVisualNode is null)
            {
                return;
            }

            _controlVisualNode.Mesh = BuildMarkerMesh(_deformer.ControlVertices);
            _anchorVisualNode.Mesh = BuildMarkerMesh(_deformer.AnchorVertices);

            _controlVisualNode.IsVisible = _isVisible && _deformer.ControlVertices.Count > 0;
            _anchorVisualNode.IsVisible = _isVisible && _deformer.AnchorVertices.Count > 0;

            if (HandleNode is not null && _deformer.ActionMode != ArapActionMode.Deform)
            {
                HandleNode.SetPose(_deformer.Pivot, Quaternion.Identity);
            }
        }

        public void SetVisibility(bool isVisible)
        {
            _isVisible = isVisible;

            if (_deformer is not null)
            {
                Refresh();
            }

            if (!isVisible && gizmoSystem.TargetNode is ArapHandleNode)
            {
                gizmoSystem.TargetNode = null;
            }
        }

        public void Clear()
        {
            if (_deformer is not null)
            {
                _deformer.SelectionChanged -= OnSelectionChanged;
            }

            _controlVisualNode?.Parent?.RemoveChild(_controlVisualNode);
            _anchorVisualNode?.Parent?.RemoveChild(_anchorVisualNode);
            HandleNode?.Parent?.RemoveChild(HandleNode);

            if (gizmoSystem.TargetNode is ArapHandleNode)
            {
                gizmoSystem.TargetNode = null;
            }

            _deformer = null;
            _parentNode = null;
            _controlVisualNode = null;
            _anchorVisualNode = null;
            HandleNode = null;
        }

        #endregion

        #region Private Logic

        private void OnSelectionChanged(object? sender, EventArgs eventArgs)
        {
            Refresh();
        }

        private Mesh BuildMarkerMesh(IEnumerable<int> vertexIndices)
        {
            if (_deformer is null)
            {
                return new Mesh([], []);
            }

            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            foreach (var vertexIndex in vertexIndices)
            {
                AppendOctahedron(vertices, indices, _deformer.GetOriginalPosition(vertexIndex), _markerRadius);
            }

            return new Mesh([.. vertices], [.. indices]);
        }

        private static void AppendOctahedron(List<Vertex> vertices, List<uint> indices, Vector3 center, float radius)
        {
            var offset = (uint)vertices.Count;
            var positions = new[]
            {
                center + Vector3.UnitY * radius,
                center - Vector3.UnitY * radius,
                center + Vector3.UnitX * radius,
                center - Vector3.UnitX * radius,
                center + Vector3.UnitZ * radius,
                center - Vector3.UnitZ * radius
            };

            foreach (var position in positions)
            {
                var normal = (position - center).Normalized();
                vertices.Add(new Vertex(position, normal));
            }

            var localIndices = new uint[]
            {
                0, 2, 4,
                0, 4, 3,
                0, 3, 5,
                0, 5, 2,
                1, 4, 2,
                1, 3, 4,
                1, 5, 3,
                1, 2, 5
            };

            foreach (var index in localIndices)
            {
                indices.Add(offset + index);
            }
        }

        #endregion
    }
}
