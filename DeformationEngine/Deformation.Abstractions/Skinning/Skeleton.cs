using OpenTK.Mathematics;

namespace Deformation.Abstractions.Skinning
{
    public sealed class Skeleton
    {
        private readonly int[] _rootBoneIndices;
        private bool _isDirty = true;

        public Skeleton(IReadOnlyList<Bone> bones)
        {
            Bones = bones;
            _rootBoneIndices = [.. bones.Where(bone => bone.ParentIndex is null).Select(bone => bone.Index)];
        }

        public IReadOnlyList<Bone> Bones { get; }
        public bool HasBones => Bones.Count > 0;

        public void MarkDirty()
        {
            _isDirty = true;
        }

        public void UpdateWorldTransforms()
        {
            if (!_isDirty && Bones.All(bone => !bone.IsDirty))
            {
                return;
            }

            foreach (var rootBoneIndex in _rootBoneIndices)
            {
                UpdateWorldTransform(rootBoneIndex, Matrix4.Identity);
            }

            _isDirty = false;
        }

        public void ResetToBindPose()
        {
            foreach (var bone in Bones)
            {
                bone.ResetToBindPose();
            }

            MarkDirty();
        }

        public void RebindToCurrentPose()
        {
            UpdateWorldTransforms();

            foreach (var bone in Bones)
            {
                var inverseWorld = bone.WorldTransform;
                inverseWorld.Invert();
                bone.RebindToCurrentPose(inverseWorld);
            }

            MarkDirty();
        }

        private void UpdateWorldTransform(int boneIndex, Matrix4 parentWorldTransform)
        {
            var bone = Bones[boneIndex];
            bone.WorldTransform = bone.LocalTransform * parentWorldTransform;
            bone.IsDirty = false;

            foreach (var childIndex in bone.Children)
            {
                UpdateWorldTransform(childIndex, bone.WorldTransform);
            }
        }
    }
}
