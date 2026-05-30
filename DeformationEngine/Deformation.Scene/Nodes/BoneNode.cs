using Deformation.Abstractions.Skinning;
using OpenTK.Mathematics;

namespace Deformation.Scene.Nodes
{
    public sealed class BoneNode : MeshNode
    {
        private bool _isSynchronizing;
        private Matrix4 _manualLocalTransform;

        public BoneNode(Bone bone)
        {
            Bone = bone;
            _manualLocalTransform = bone.LocalTransform;
            ApplyBoneTransform();
        }

        public Bone Bone { get; }

        public override Matrix4 LocalTransform => _manualLocalTransform;

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

        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();

            if (_isSynchronizing)
            {
                return;
            }

            _manualLocalTransform = base.LocalTransform;
            Bone.LocalTransform = _manualLocalTransform;
        }
    }
}