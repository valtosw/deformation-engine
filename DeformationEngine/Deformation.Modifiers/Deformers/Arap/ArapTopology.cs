using Deformation.Abstractions.Comparers;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;

namespace Deformation.Modifiers.Deformers.Arap
{
    internal sealed class ArapTopology
    {
        private ArapTopology(int[][] neighbors, int[][] weldedVertexGroups)
        {
            Neighbors = neighbors;
            WeldedVertexGroups = weldedVertexGroups;
        }

        public int[][] Neighbors { get; }
        public int[][] WeldedVertexGroups { get; }

        public static ArapTopology Build(Mesh mesh)
        {
            var neighborSets = Enumerable.Range(0, mesh.Vertices.Length)
                .Select(_ => new HashSet<int>())
                .ToArray();

            if (mesh.Topology == MeshTopology.Triangles)
            {
                for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
                {
                    AddEdge((int)mesh.Indices[index], (int)mesh.Indices[index + 1]);
                    AddEdge((int)mesh.Indices[index + 1], (int)mesh.Indices[index + 2]);
                    AddEdge((int)mesh.Indices[index + 2], (int)mesh.Indices[index]);
                }
            }
            else
            {
                for (var index = 0; index + 1 < mesh.Indices.Length; index += 2)
                {
                    AddEdge((int)mesh.Indices[index], (int)mesh.Indices[index + 1]);
                }
            }

            var positionGroups = mesh.Vertices
                .Select((vertex, index) => (vertex.Position, Index: index))
                .GroupBy(item => item.Position, new Vector3EqualityComparer())
                .Select(group => group.Select(item => item.Index).ToArray())
                .ToArray();

            var weldedVertexGroups = new int[mesh.Vertices.Length][];

            foreach (var group in positionGroups)
            {
                foreach (var index in group)
                {
                    weldedVertexGroups[index] = group;
                }

                if (group.Length <= 1)
                {
                    continue;
                }

                for (var first = 0; first < group.Length; first++)
                {
                    for (var second = first + 1; second < group.Length; second++)
                    {
                        AddEdge(group[first], group[second]);
                    }
                }
            }

            return new ArapTopology(neighborSets.Select(set => set.ToArray()).ToArray(), weldedVertexGroups);

            void AddEdge(int indexA, int indexB)
            {
                if (indexA == indexB ||
                    indexA < 0 ||
                    indexB < 0 ||
                    indexA >= mesh.Vertices.Length ||
                    indexB >= mesh.Vertices.Length)
                {
                    return;
                }

                neighborSets[indexA].Add(indexB);
                neighborSets[indexB].Add(indexA);
            }
        }
    }
}
