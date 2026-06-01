using OpenTK.Mathematics;

namespace Deformation.Scene.Nodes
{
    public sealed class ArapHandleNode(Action<Vector3, Quaternion> onMoved) : MeshNode
    {
        #region Fields

        private bool _suppressMovementNotification;

        #endregion

        #region Public Logic

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            _suppressMovementNotification = true;
            Translation = position;
            Rotation = rotation;
            _suppressMovementNotification = false;
        }

        #endregion

        #region Protected Logic

        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();

            if (!_suppressMovementNotification)
            {
                onMoved(Translation, Rotation);
            }
        }

        #endregion
    }
}
