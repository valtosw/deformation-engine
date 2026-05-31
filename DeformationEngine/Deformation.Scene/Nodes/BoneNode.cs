using Deformation.Abstractions.Skinning;
using OpenTK.Mathematics;

namespace Deformation.Scene.Nodes
{
    public sealed class BoneNode : MeshNode
    {
        #region Fields

        private readonly Action? _onTransformChanged;
        private bool _isSynchronizing;
        private Matrix4 _manualLocalTransform;

        #endregion

        #region Constructors

        public BoneNode(Bone bone, Action? onTransformChanged = null)
        {
            Bone = bone;
            _onTransformChanged = onTransformChanged;
            _manualLocalTransform = bone.LocalTransform;
            ApplyBoneTransform();
        }

        #endregion

        #region Properties

        public Bone Bone { get; }

        public override Matrix4 LocalTransform => _manualLocalTransform;

        #endregion

        #region Public Logic

        public void ApplyBoneTransform()
        {
            _isSynchronizing = true;

            _manualLocalTransform = Bone.LocalTransform;
            InvalidateLocalTransform();

            Translation = _manualLocalTransform.ExtractTranslation();
            Rotation = _manualLocalTransform.ExtractRotation();
            Scale = _manualLocalTransform.ExtractScale();

            _isSynchronizing = false;
        }

        #endregion

        #region Protected Logic

        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();

            if (_isSynchronizing)
            {
                return;
            }

            _manualLocalTransform = base.LocalTransform;
            Bone.LocalTransform = _manualLocalTransform;
            _onTransformChanged?.Invoke();
        }

        #endregion
    }
}