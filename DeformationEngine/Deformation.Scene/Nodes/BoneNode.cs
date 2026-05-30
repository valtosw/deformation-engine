using Deformation.Abstractions.Skinning;

namespace Deformation.Scene.Nodes
{
    public sealed class BoneNode : MeshNode
    {
        private bool _isSynchronizing;

        public BoneNode(Bone bone)
        {
            Bone = bone;
            ApplyBoneTransform();
        }

        public Bone Bone { get; }

        public void ApplyBoneTransform()
        {
            _isSynchronizing = true;

            Translation = Bone.LocalTransform.ExtractTranslation();
            Rotation = Bone.LocalTransform.ExtractRotation();
            Scale = Bone.LocalTransform.ExtractScale();

            _isSynchronizing = false;
        }

        protected override void OnTransformChanged()
        {
            base.OnTransformChanged();

            if (_isSynchronizing)
            {
                return;
            }

            Bone.LocalTransform = LocalTransform;
        }
    }
}
