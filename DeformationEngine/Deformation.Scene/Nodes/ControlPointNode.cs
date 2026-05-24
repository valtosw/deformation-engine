using OpenTK.Mathematics;

namespace Deformation.Scene.Nodes
{
    public sealed class ControlPointNode(int indexX, int indexY, int indexZ, Action<int, int, int, Vector3> onMoved) : MeshNode
    {
        #region Properties

        public int IndexX { get; } = indexX;
        public int IndexY { get; } = indexY;
        public int IndexZ { get; } = indexZ;

        #endregion

        #region Protected Logic

        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();
            onMoved(IndexX, IndexY, IndexZ, Translation);
        }

        #endregion
    }
}