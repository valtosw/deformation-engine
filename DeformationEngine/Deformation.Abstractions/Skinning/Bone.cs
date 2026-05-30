using OpenTK.Mathematics;

namespace Deformation.Abstractions.Skinning
{
    public sealed class Bone
    {
        private Matrix4 _localTransform;

        public Bone(int index, string name, int? parentIndex, Matrix4 localTransform, Matrix4 inverseBindTransform)
        {
            Index = index;
            Name = name;
            ParentIndex = parentIndex;
            BindLocalTransform = localTransform;
            _localTransform = localTransform;
            InverseBindTransform = inverseBindTransform;
        }

        public int Index { get; }
        public string Name { get; }
        public int? ParentIndex { get; }
        public List<int> Children { get; } = [];
        public Matrix4 BindLocalTransform { get; private set; }
        public Matrix4 InverseBindTransform { get; private set; }
        public Matrix4 WorldTransform { get; internal set; } = Matrix4.Identity;

        public Matrix4 LocalTransform
        {
            get => _localTransform;
            set
            {
                _localTransform = value;
                IsDirty = true;
            }
        }

        public bool IsDirty { get; internal set; } = true;

        public void ResetToBindPose()
        {
            LocalTransform = BindLocalTransform;
        }

        public void RebindToCurrentPose(Matrix4 inverseCurrentWorldTransform)
        {
            BindLocalTransform = LocalTransform;
            InverseBindTransform = inverseCurrentWorldTransform;
            IsDirty = true;
        }
    }
}
