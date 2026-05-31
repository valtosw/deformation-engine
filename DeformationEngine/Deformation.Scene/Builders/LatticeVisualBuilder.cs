using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;

namespace Deformation.Scene.Builders
{
    public sealed class LatticeVisualBuilder(IGizmoSystem gizmoSystem) : ILatticeVisualBuilder
    {
        #region Fields

        private readonly List<ControlPointNode> _controlPointNodes = [];
        private MeshNode? _latticeNode;

        #endregion

        #region Properties

        public IReadOnlyList<ControlPointNode> ControlPointNodes
        {
            get
            {
                return _controlPointNodes;
            }
        }

        #endregion

        #region Public Logic

        public void Build(MeshNode parentNode, FfdDeformer deformer, float targetSphereRadius, bool isVisible, Action onLatticeChanged)
        {
            var lattice = deformer.Lattice;

            if (lattice is null)
            {
                return;
            }

            gizmoSystem.TargetNode = null;

            var controlPointRadius = MathF.Max(0.01f, targetSphereRadius * 0.025f);
            var controlPointMesh = MeshFactory.CreateSphere(controlPointRadius, rings: 8, segments: 12, Vector3.Zero);

            var edgeCount =
                (lattice.ResolutionX - 1) * lattice.ResolutionY * lattice.ResolutionZ +
                lattice.ResolutionX * (lattice.ResolutionY - 1) * lattice.ResolutionZ +
                lattice.ResolutionX * lattice.ResolutionY * (lattice.ResolutionZ - 1);

            var lineVertices = new List<Vertex>(lattice.ControlPointCount);
            var lineIndices = new List<uint>(edgeCount * 2);

            for (var indexX = 0; indexX < lattice.ResolutionX; indexX++)
            {
                for (var indexY = 0; indexY < lattice.ResolutionY; indexY++)
                {
                    for (var indexZ = 0; indexZ < lattice.ResolutionZ; indexZ++)
                    {
                        var position = lattice.GetControlPoint(indexX, indexY, indexZ);

                        var controlPointNode = new ControlPointNode(indexX, indexY, indexZ, (movedX, movedY, movedZ, newPosition) =>
                        {
                            deformer.UpdateControlPoint(movedX, movedY, movedZ, newPosition);

                            if (_latticeNode?.Mesh is not null)
                            {
                                var flatIndex = lattice.GetFlatIndex(movedX, movedY, movedZ);
                                _latticeNode.Mesh.Vertices[flatIndex].Position = newPosition;
                                _latticeNode.ApplyDeformers();
                            }

                            onLatticeChanged();
                        })
                        {
                            Mesh = controlPointMesh,
                            Color = ColorConstants.ZAxisColor,
                            IsVisible = isVisible,
                            IgnoreDepth = false,
                            ForceSolid = true
                        };

                        controlPointNode.SetPositionFromLattice(position);

                        _controlPointNodes.Add(controlPointNode);
                        parentNode.AddChild(controlPointNode);

                        lineVertices.Add(new Vertex(position));

                        var currentIndex = (uint)lattice.GetFlatIndex(indexX, indexY, indexZ);

                        if (indexX < lattice.ResolutionX - 1)
                        {
                            lineIndices.Add(currentIndex);
                            lineIndices.Add((uint)lattice.GetFlatIndex(indexX + 1, indexY, indexZ));
                        }

                        if (indexY < lattice.ResolutionY - 1)
                        {
                            lineIndices.Add(currentIndex);
                            lineIndices.Add((uint)lattice.GetFlatIndex(indexX, indexY + 1, indexZ));
                        }

                        if (indexZ < lattice.ResolutionZ - 1)
                        {
                            lineIndices.Add(currentIndex);
                            lineIndices.Add((uint)lattice.GetFlatIndex(indexX, indexY, indexZ + 1));
                        }
                    }
                }
            }

            var lineMesh = new Mesh([.. lineVertices], [.. lineIndices])
            {
                Topology = MeshTopology.Lines
            };

            _latticeNode = new MeshNode
            {
                Mesh = lineMesh,
                Color = new Vector3(0.6f, 0.6f, 0.6f),
                IsVisible = isVisible,
                IgnoreDepth = false,
                ForceWireframe = true
            };

            parentNode.AddChild(_latticeNode);
        }

        public void UpdateFromLattice(FfdDeformer deformer)
        {
            var lattice = deformer.Lattice;

            if (lattice is null)
            {
                return;
            }

            foreach (var controlPointNode in _controlPointNodes)
            {
                var position = lattice.GetControlPoint(controlPointNode.IndexX, controlPointNode.IndexY, controlPointNode.IndexZ);
                controlPointNode.SetPositionFromLattice(position);
            }

            if (_latticeNode?.Mesh is null)
            {
                return;
            }

            for (var indexX = 0; indexX < lattice.ResolutionX; indexX++)
            {
                for (var indexY = 0; indexY < lattice.ResolutionY; indexY++)
                {
                    for (var indexZ = 0; indexZ < lattice.ResolutionZ; indexZ++)
                    {
                        var flatIndex = lattice.GetFlatIndex(indexX, indexY, indexZ);
                        _latticeNode.Mesh.Vertices[flatIndex].Position = lattice.GetControlPoint(indexX, indexY, indexZ);
                    }
                }
            }

            _latticeNode.ApplyDeformers();
        }

        public void SetVisibility(bool isVisible)
        {
            foreach (var controlPointNode in _controlPointNodes)
            {
                controlPointNode.IsVisible = isVisible;
            }

            _latticeNode?.IsVisible = isVisible;

            if (!isVisible && gizmoSystem.TargetNode is ControlPointNode)
            {
                gizmoSystem.TargetNode = null;
            }
        }

        public void Clear()
        {
            foreach (var controlPointNode in _controlPointNodes)
            {
                controlPointNode.Parent?.RemoveChild(controlPointNode);
            }

            _controlPointNodes.Clear();
            _latticeNode?.Parent?.RemoveChild(_latticeNode);
            _latticeNode = null;

            if (gizmoSystem.TargetNode is ControlPointNode)
            {
                gizmoSystem.TargetNode = null;
            }
        }

        #endregion
    }
}