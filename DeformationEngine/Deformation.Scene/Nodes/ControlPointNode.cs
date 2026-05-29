using OpenTK.Mathematics;

namespace Deformation.Scene.Nodes
{
    public sealed class ControlPointNode(int indexX, int indexY, int indexZ, Action<int, int, int, Vector3> onMoved) : MeshNode
    {
        #region Fields

        private bool _suppressMovementNotification;

        #endregion

        #region Properties

        public int IndexX { get; } = indexX;
        public int IndexY { get; } = indexY;
        public int IndexZ { get; } = indexZ;

        #endregion

        #region Public Logic

        public void SetPositionFromLattice(Vector3 position)
        {
            _suppressMovementNotification = true;
            Translation = position;
            _suppressMovementNotification = false;
        }

        #endregion

        #region Protected Logic

        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();

            if (_suppressMovementNotification)
            {
                return;
            }

            onMoved(IndexX, IndexY, IndexZ, Translation);
        }

        #endregion
    }
}
