using OpenTK.Mathematics;

namespace Deformation.Abstractions.Geometry
{
    public sealed class SpatialHash3<T>(Vector3 origin, float cellSize)
    {
        private readonly Dictionary<(int X, int Y, int Z), List<T>> _itemsByCell = [];

        public void Add(Vector3 position, T item)
        {
            var cell = GetCell(position);

            if (!_itemsByCell.TryGetValue(cell, out var items))
            {
                items = [];
                _itemsByCell[cell] = items;
            }

            items.Add(item);
        }

        public IEnumerable<T> GetNearby(Vector3 position)
        {
            var cell = GetCell(position);

            for (var offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (var offsetZ = -1; offsetZ <= 1; offsetZ++)
                    {
                        var nearbyCell = (cell.X + offsetX, cell.Y + offsetY, cell.Z + offsetZ);

                        if (!_itemsByCell.TryGetValue(nearbyCell, out var items))
                        {
                            continue;
                        }

                        foreach (var item in items)
                        {
                            yield return item;
                        }
                    }
                }
            }
        }

        private (int X, int Y, int Z) GetCell(Vector3 position)
        {
            var offset = position - origin;

            return (
                (int)MathF.Floor(offset.X / cellSize),
                (int)MathF.Floor(offset.Y / cellSize),
                (int)MathF.Floor(offset.Z / cellSize));
        }
    }
}
