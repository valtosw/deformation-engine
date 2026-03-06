using OpenTK.Mathematics;
using Visualization.Abstractions.Geometry;

namespace Visualization.Scene.Nodes
{
    public sealed class MeshNode : SceneNode
    {
        private Mesh? _mesh;

        public Mesh? Mesh
        {
            get => _mesh;
            set
            {
                if (ReferenceEquals(_mesh, value))
                    return;

                _mesh = value;
                InvalidateBoundingBox();
            }
        }

        public void NotifyGeometryChanged() => InvalidateBoundingBox();

        protected override IEnumerable<Vector3> EnumerateLocalPoints()
        {
            if (_mesh is null)
                yield break;

            foreach (var vertex in _mesh.Vertices)
                yield return vertex.Position;
        }
    }
}
