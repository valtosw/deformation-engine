using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;
using Rendering.Abstractions;

namespace Deformation.Scene.Nodes
{
    public class SceneNode
    {
        #region Fields

        private readonly List<SceneNode> _children = [];
        private SceneNode? _parent;

        private Vector3 _translation = Vector3.Zero;
        private Quaternion _rotation = Quaternion.Identity;
        private Vector3 _scale = Vector3.One;
        private bool _isVisible = true;

        private Matrix4? _cachedLocalTransform;
        private Matrix4? _cachedWorldTransform;
        private AxisAlignedBoundingBox? _cachedBoundingBox;

        #endregion

        #region Properties

        public IReadOnlyList<SceneNode> Children => _children;

        public SceneNode? Parent
        {
            get => _parent;
            set
            {
                if (_parent == value)
                {
                    return;
                }

                _parent = value;
                InvalidateWorldTransform();
            }
        }

        public Vector3 Translation
        {
            get => _translation;
            set
            {
                if (_translation != value)
                {
                    _translation = value;
                    InvalidateLocalTransform();
                    OnTransformChanged();
                }
            }
        }

        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                if (_rotation != value)
                {
                    _rotation = value;
                    InvalidateLocalTransform();
                    OnTransformChanged();
                }
            }
        }

        public Vector3 Scale
        {
            get => _scale;
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    InvalidateLocalTransform();
                    OnTransformChanged();
                }
            }
        }

        public virtual Matrix4 LocalTransform
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
                _cachedWorldTransform ??= (LocalTransform * Parent?.WorldTransform) ?? LocalTransform;
                return _cachedWorldTransform.Value;
            }
        }

        public virtual AxisAlignedBoundingBox BoundingBox
        {
            get
            {
                if (_cachedBoundingBox is not null)
                {
                    return _cachedBoundingBox;
                }

                AxisAlignedBoundingBox? boundingBox = null;

                if (LocalBoundingBox is { } localBoundingBox)
                {
                    var min = localBoundingBox.Min;
                    var max = localBoundingBox.Max;

                    var corners = new Vector3[]
                    {
                        new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z),
                        new(min.X, max.Y, min.Z), new(max.X, max.Y, min.Z),
                        new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z),
                        new(min.X, max.Y, max.Z), new(max.X, max.Y, max.Z)
                    };

                    var worldCorners = corners.Select(c => WorldTransform.TransformPoint(c));
                    boundingBox = AxisAlignedBoundingBox.FromPoints(worldCorners);
                }

                boundingBox = _children
                    .Where(child => child.IsVisible)
                    .Aggregate(boundingBox, (current, child) => AxisAlignedBoundingBox.Combine(current, child.BoundingBox));

                return _cachedBoundingBox = boundingBox
                    ?? new AxisAlignedBoundingBox(WorldTransform.TransformPoint(Vector3.Zero), WorldTransform.TransformPoint(Vector3.Zero));
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value)
                {
                    return;
                }

                _isVisible = value;
                InvalidateBoundingBox();
            }
        }

        protected virtual AxisAlignedBoundingBox? LocalBoundingBox => null;

        #endregion

        #region Public Logic

        public void AddChild(SceneNode child)
        {
            child.Parent?.RemoveChild(child);

            _children.Add(child);
            child.Parent = this;
        }

        public void RemoveChild(SceneNode child)
        {
            if (_children.Remove(child))
            {
                child.Parent = null;
            }
        }

        public virtual void OnRendering(IRenderingContext renderingContext)
        {
            if (!IsVisible)
            {
                return;
            }

            renderingContext.SetMatrix("model", WorldTransform);

            foreach (var child in _children)
            {
                child.OnRendering(renderingContext);
            }
        }

        #endregion

        #region Private Logic

        protected virtual void OnTransformChanged() { }

        private protected void InvalidateBoundingBox()
        {
            _cachedBoundingBox = null;
            Parent?.InvalidateBoundingBox();
        }

        protected void InvalidateLocalTransform()
        {
            _cachedLocalTransform = null;
            InvalidateWorldTransform();
        }

        private void InvalidateWorldTransform()
        {
            _cachedWorldTransform = null;
            InvalidateBoundingBox();

            foreach (var child in _children)
            {
                child.InvalidateWorldTransform();
            }
        }

        #endregion
    }
}