using OpenTK.Mathematics;
using Visualization.Abstractions.Extensions;
using Visualization.Abstractions.Geometry;
using Visualization.Rendering.Abstractions;

namespace Visualization.Scene.Nodes
{
    public class SceneNode
    {
        private readonly List<SceneNode> _children = [];
        private SceneNode? _parent;

        private Vector3 _translation = Vector3.Zero;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;

        private Matrix4? _cachedLocalTransform;
        private Matrix4? _cachedWorldTransform;
        private AxisAlignedBoundingBox? _cachedBoundingBox;

        public IReadOnlyCollection<SceneNode> Children => _children;

        public SceneNode? Parent
        {
            get => _parent;
            set
            {
                if (_parent == value)
                    return;

                _parent = value;
                InvalidateWorldTransform();
            }
        }

        public Vector3 Translation
        {
            get => _translation;
            set
            {
                _translation = value;
                InvalidateLocalTransform();
            }
        }

        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                InvalidateLocalTransform();
            }
        }

        public Vector3 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                InvalidateLocalTransform();
            }
        }

        public Matrix4 LocalTransform
        {
            get
            {
                _cachedLocalTransform ??= Matrix4.CreateScale(_scale) * Matrix4.CreateFromQuaternion(_rotation) * Matrix4.CreateTranslation(_translation);
                return _cachedLocalTransform.Value;
            }
        }

        public Matrix4 WorldTransform
        {
            get
            {
                _cachedWorldTransform ??= (Parent?.WorldTransform * LocalTransform) ?? LocalTransform;
                return _cachedWorldTransform.Value;
            }
        }

        public virtual AxisAlignedBoundingBox BoundingBox
        {
            get
            {
                if (_cachedBoundingBox is not null) 
                    return _cachedBoundingBox;

                var worldPoints = EnumerateLocalPoints().Select(point => WorldTransform.TransformPoint(point));
                var boundingBox = AxisAlignedBoundingBox.FromPoints(worldPoints);
                boundingBox = _children.Aggregate(boundingBox, (current, child) => AxisAlignedBoundingBox.Combine(current, child.BoundingBox));

                return _cachedBoundingBox = boundingBox;
            }
        }

        public void AddChild(SceneNode child)
        {
            child.Parent?.RemoveChild(child);

            _children.Add(child);
            child.Parent = this;
        }

        public void RemoveChild(SceneNode child)
        {
            if (_children.Remove(child))
                child.Parent = null;
        }

        public virtual void OnRendering(IRenderingContext renderingContext)
        {
            renderingContext.SetMatrix("model", WorldTransform);

            foreach (var child in _children)
                child.OnRendering(renderingContext);
        }

        protected virtual IEnumerable<Vector3> EnumerateLocalPoints() => [];

        private protected void InvalidateBoundingBox()
        {
            _cachedBoundingBox = null;
            Parent?.InvalidateBoundingBox();
        }

        private void InvalidateLocalTransform()
        {
            _cachedLocalTransform = null;
            InvalidateWorldTransform();
        }

        private void InvalidateWorldTransform()
        {
            _cachedWorldTransform = null;
            InvalidateBoundingBox();

            foreach (var child in _children)
                child.InvalidateWorldTransform();
        }
    }
}
