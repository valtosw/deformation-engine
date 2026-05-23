using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Geometry;
using OpenTK.Mathematics;

namespace Deformation.Scene.Nodes
{
    public sealed class GizmoNode : SceneNode
    {
        #region Fields

        private readonly SceneNode _translateGroup = new();
        private readonly SceneNode _rotateGroup = new();
        private readonly SceneNode _scaleGroup = new();

        #endregion

        #region Constructors

        public GizmoNode()
        {
            InitializeTranslateGroup();
            InitializeRotateGroup();
            InitializeScaleGroup();

            AddChild(_translateGroup);
            AddChild(_rotateGroup);
            AddChild(_scaleGroup);
        }

        #endregion

        #region Public Logic

        public void SetMode(GizmoMode mode)
        {
            _translateGroup.IsVisible = mode == GizmoMode.Translate;
            _rotateGroup.IsVisible = mode == GizmoMode.Rotate;
            _scaleGroup.IsVisible = mode == GizmoMode.Scale;
        }

        public MeshNode GetActiveXAxis(GizmoMode mode)
        {
            return mode switch
            {
                GizmoMode.Translate => (MeshNode)_translateGroup.Children[0],
                GizmoMode.Rotate => (MeshNode)_rotateGroup.Children[0],
                GizmoMode.Scale => (MeshNode)_scaleGroup.Children[0],
                _ => (MeshNode)_translateGroup.Children[0]
            };
        }

        public MeshNode GetActiveYAxis(GizmoMode mode)
        {
            return mode switch
            {
                GizmoMode.Translate => (MeshNode)_translateGroup.Children[1],
                GizmoMode.Rotate => (MeshNode)_rotateGroup.Children[1],
                GizmoMode.Scale => (MeshNode)_scaleGroup.Children[1],
                _ => (MeshNode)_translateGroup.Children[1]
            };
        }

        public MeshNode GetActiveZAxis(GizmoMode mode)
        {
            return mode switch
            {
                GizmoMode.Translate => (MeshNode)_translateGroup.Children[2],
                GizmoMode.Rotate => (MeshNode)_rotateGroup.Children[2],
                GizmoMode.Scale => (MeshNode)_scaleGroup.Children[2],
                _ => (MeshNode)_translateGroup.Children[2]
            };
        }

        #endregion

        #region Private Logic

        private void InitializeTranslateGroup()
        {
            var cylinder = MeshFactory.CreateCylinder(0.04f, 1.0f, 16, Vector3.Zero);
            var cone = MeshFactory.CreateCone(0.12f, 0.3f, 16, new Vector3(0, 1.0f, 0));
            var arrowMesh = MeshFactory.Combine(cylinder, cone);

            _translateGroup.AddChild(CreateGizmoPart(arrowMesh, new Vector3(1, 0.2f, 0.2f), Quaternion.FromEulerAngles(0, 0, -MathHelper.PiOver2)));
            _translateGroup.AddChild(CreateGizmoPart(arrowMesh, new Vector3(0.2f, 1, 0.2f), Quaternion.Identity));
            _translateGroup.AddChild(CreateGizmoPart(arrowMesh, new Vector3(0.2f, 0.5f, 1), Quaternion.FromEulerAngles(MathHelper.PiOver2, 0, 0)));
        }

        private void InitializeRotateGroup()
        {
            var torusMesh = MeshFactory.CreateTorus(1.0f, 0.04f, 32, 12);

            _rotateGroup.AddChild(CreateGizmoPart(torusMesh, new Vector3(1, 0.2f, 0.2f), Quaternion.FromEulerAngles(0, 0, MathHelper.PiOver2)));
            _rotateGroup.AddChild(CreateGizmoPart(torusMesh, new Vector3(0.2f, 1, 0.2f), Quaternion.Identity));
            _rotateGroup.AddChild(CreateGizmoPart(torusMesh, new Vector3(0.2f, 0.5f, 1), Quaternion.FromEulerAngles(MathHelper.PiOver2, 0, 0)));
        }

        private void InitializeScaleGroup()
        {
            var cylinder = MeshFactory.CreateCylinder(0.04f, 1.0f, 16, Vector3.Zero);
            var box = MeshFactory.CreateBox(new Vector3(0.2f), new Vector3(0, 1.1f, 0));
            var scaleHandleMesh = MeshFactory.Combine(cylinder, box);

            _scaleGroup.AddChild(CreateGizmoPart(scaleHandleMesh, new Vector3(1, 0.2f, 0.2f), Quaternion.FromEulerAngles(0, 0, -MathHelper.PiOver2)));
            _scaleGroup.AddChild(CreateGizmoPart(scaleHandleMesh, new Vector3(0.2f, 1, 0.2f), Quaternion.Identity));
            _scaleGroup.AddChild(CreateGizmoPart(scaleHandleMesh, new Vector3(0.2f, 0.5f, 1), Quaternion.FromEulerAngles(MathHelper.PiOver2, 0, 0)));
        }

        private static MeshNode CreateGizmoPart(Mesh mesh, Vector3 color, Quaternion rotation)
        {
            return new MeshNode
            {
                Mesh = mesh,
                Color = color,
                Rotation = rotation,
                ForceSolid = true,
                IgnoreDepth = false
            };
        }

        #endregion
    }
}