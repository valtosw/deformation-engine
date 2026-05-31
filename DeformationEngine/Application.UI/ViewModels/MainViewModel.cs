using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Abstractions.Extensions;
using Deformation.Abstractions.Geometry;
using Deformation.Interaction;
using Deformation.Interaction.Input;
using Deformation.IO.Abstractions;
using Deformation.Scene.Abstractions;
using Deformation.Scene.Nodes;
using OpenTK.Mathematics;
using Rendering.Abstractions;
using System.IO;
using System.Windows;

namespace Application.UI.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        #region Fields

        private static readonly HashSet<string> SkinningExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".gltf",
            ".glb",
            ".dae",
            ".gbd"
        };

        private readonly IMeshImporterFactory _meshImporterFactory;
        private readonly List<ControlPointNode> _ffdControlPoints = [];
        private readonly List<BoneNode> _boneNodes = [];

        private MeshNode? _activeMeshNode;
        private MeshNode? _latticeNode;
        private MeshNode? _skeletonLineNode;
        private bool _hasModel;
        private bool _hasSkinning;
        private DeformationMode _selectedMode = DeformationMode.Basic;

        #endregion

        #region Constructors

        public MainViewModel(ControllerEngine engine, ICameraSystem cameraSystem, IGizmoSystem gizmoSystem, IMeshImporterFactory meshImporterFactory)
        {
            Engine = engine;
            CameraSystem = cameraSystem;
            GizmoSystem = gizmoSystem;
            _meshImporterFactory = meshImporterFactory;

            Gizmo = new GizmoViewModel(GizmoSystem);

            Engine.RegisterController(new CameraKeyboardController(CameraSystem, Engine));
            Engine.RegisterController(new FfdSelectionController(CameraSystem, GizmoSystem, () => SelectedMode == DeformationMode.Ffd, () => _ffdControlPoints));
            Engine.RegisterController(new LbsSelectionController(CameraSystem, GizmoSystem, () => SelectedMode == DeformationMode.LinearBlendSkinning, () => _boneNodes));
            Engine.RegisterController(new GizmoController(GizmoSystem, CameraSystem));
            Engine.RegisterController(new CameraMouseController(CameraSystem));

            Engine.RootNode.AddChild(GizmoSystem.GizmoNode);
        }

        #endregion

        #region Properties

        public ControllerEngine Engine { get; }
        public ICameraSystem CameraSystem { get; }
        public IGizmoSystem GizmoSystem { get; }

        public DeformerViewModel Deformers { get; } = new();
        public GizmoViewModel Gizmo { get; }

        public Func<string, bool>? RequestConfirmation { get; set; }
        public Action<string>? RequestWarning { get; set; }

        public bool HasModel
        {
            get
            {
                return _hasModel;
            }
            private set
            {
                if (SetProperty(ref _hasModel, value))
                {
                    ApplySelectedModeState();
                }
            }
        }

        public bool HasSkinning
        {
            get => _hasSkinning;
            private set
            {
                if (SetProperty(ref _hasSkinning, value))
                {
                    OnPropertyChanged(nameof(AvailableModes));
                }
            }
        }

        public DeformationMode SelectedMode
        {
            get
            {
                return _selectedMode;
            }
            set
            {
                if (_selectedMode == value)
                {
                    return;
                }

                if (value == DeformationMode.LinearBlendSkinning && !HasSkinning)
                {
                    OnPropertyChanged();
                    return;
                }

                if (HasUnbakedChanges())
                {
                    var shouldProceed = RequestConfirmation?.Invoke("You haven't baked the deformation. Switching deformation type will discard the current unbaked changes. Do you wish to proceed?");

                    if (shouldProceed != true)
                    {
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            OnPropertyChanged(nameof(SelectedMode));
                        }));

                        return;
                    }

                    RestoreParameters();
                }

                _selectedMode = value;
                ApplySelectedModeState();

                OnPropertyChanged();
                OnPropertyChanged(nameof(BasicPanelVisibility));
                OnPropertyChanged(nameof(TwistPanelVisibility));
                OnPropertyChanged(nameof(BendPanelVisibility));
                OnPropertyChanged(nameof(FfdPanelVisibility));
                OnPropertyChanged(nameof(LbsPanelVisibility));
            }
        }

        public Visibility BasicPanelVisibility
        {
            get
            {
                return SelectedMode == DeformationMode.Basic ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility TwistPanelVisibility
        {
            get
            {
                return SelectedMode == DeformationMode.Twist ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility BendPanelVisibility
        {
            get
            {
                return SelectedMode == DeformationMode.Bend ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility FfdPanelVisibility
        {
            get
            {
                return SelectedMode == DeformationMode.Ffd ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility LbsPanelVisibility
        {
            get
            {
                return SelectedMode == DeformationMode.LinearBlendSkinning ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public IEnumerable<DeformationMode> AvailableModes
        {
            get
            {
                return Enum.GetValues<DeformationMode>().Where(mode => mode != DeformationMode.LinearBlendSkinning || HasSkinning);
            }
        }

        #endregion

        #region Public Logic

        public void InitializeRendering(IRenderingContext renderingContext)
        {
            Engine.Initialize(renderingContext);
        }

        public void Resize(int width, int height)
        {
            CameraSystem.SetViewport(width, height);
        }

        public void Render(float deltaTime)
        {
            if (SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                UpdateSkeletonLinesFromBones();
                _activeMeshNode?.ApplyDeformers();
            }

            Engine.UpdateAndRender(deltaTime, CameraSystem.ViewMatrix, CameraSystem.ProjectionMatrix);
        }

        public void ProcessInput(IInputEvent inputEvent)
        {
            Engine.ProcessInput(inputEvent);
        }

        public void LoadMesh(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var importer = _meshImporterFactory.GetImporter(extension);

            var mesh = importer.Load(filePath);

            ClearFfdState();
            ClearSkeletonVisuals();

            if (_activeMeshNode is not null)
            {
                Engine.RootNode.RemoveChild(_activeMeshNode);
            }

            _activeMeshNode = new MeshNode { Mesh = mesh };

            _activeMeshNode.AddDeformer(Deformers.TwistDeformer);
            _activeMeshNode.AddDeformer(Deformers.BendDeformer);
            _activeMeshNode.AddDeformer(Deformers.FfdDeformer);
            _activeMeshNode.AddDeformer(Deformers.LbsDeformer);
            Deformers.ActiveMeshNode = _activeMeshNode;

            Engine.RootNode.AddChild(_activeMeshNode);

            Engine.RootNode.RemoveChild(GizmoSystem.GizmoNode);
            Engine.RootNode.AddChild(GizmoSystem.GizmoNode);

            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);
            CameraSystem.ZoomToFit();

            HasSkinning = mesh.Skinning?.CanSkin == true;
            WarnIfMissingSkinning(extension, HasSkinning);

            if (!HasSkinning && SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                _selectedMode = DeformationMode.Basic;
                OnPropertyChanged(nameof(SelectedMode));
            }

            HasModel = true;

            if (HasSkinning)
            {
                SetupSkeletonVisuals(mesh);
            }

            ApplySelectedModeState();
        }

        public void SetupFfdLattice()
        {
            if (_activeMeshNode?.Mesh is null)
            {
                return;
            }

            ClearFfdVisuals();

            Deformers.FfdDeformer.Initialize(_activeMeshNode.Mesh, Deformers.FfdResolutionX, Deformers.FfdResolutionY, Deformers.FfdResolutionZ);

            var lattice = Deformers.FfdDeformer.Lattice;

            if (lattice is null)
            {
                return;
            }

            Gizmo.Mode = GizmoMode.Translate;
            GizmoSystem.TargetNode = null;

            var controlPointRadius = MathF.Max(0.01f, CameraSystem.TargetSphere.Radius * 0.025f);
            var controlPointMesh = MeshFactory.CreateSphere(controlPointRadius, rings: 8, segments: 12, Vector3.Zero);

            var edgeCount =
                (lattice.ResolutionX - 1) * lattice.ResolutionY * lattice.ResolutionZ +
                lattice.ResolutionX * (lattice.ResolutionY - 1) * lattice.ResolutionZ +
                lattice.ResolutionX * lattice.ResolutionY * (lattice.ResolutionZ - 1);

            var lineVertices = new List<Vertex>(lattice.ControlPointCount);
            var lineIndices = new List<uint>(edgeCount * 2);

            for (var indexX = 0; indexX < lattice.ResolutionX; indexX++)
            {
                for (var indexY = 0; indexY < lattice.ResolutionY; indexY++)
                {
                    for (var indexZ = 0; indexZ < lattice.ResolutionZ; indexZ++)
                    {
                        var position = lattice.GetControlPoint(indexX, indexY, indexZ);

                        var controlPointNode = new ControlPointNode(indexX, indexY, indexZ, OnControlPointMoved)
                        {
                            Mesh = controlPointMesh,
                            Color = ColorConstants.ZAxisColor,
                            IsVisible = SelectedMode == DeformationMode.Ffd,
                            IgnoreDepth = false,
                            ForceSolid = true
                        };

                        controlPointNode.SetPositionFromLattice(position);

                        _ffdControlPoints.Add(controlPointNode);
                        _activeMeshNode.AddChild(controlPointNode);

                        lineVertices.Add(new Vertex(position));

                        var currentIndex = (uint)lattice.GetFlatIndex(indexX, indexY, indexZ);

                        if (indexX < lattice.ResolutionX - 1)
                        {
                            lineIndices.Add(currentIndex);
                            lineIndices.Add((uint)lattice.GetFlatIndex(indexX + 1, indexY, indexZ));
                        }

                        if (indexY < lattice.ResolutionY - 1)
                        {
                            lineIndices.Add(currentIndex);
                            lineIndices.Add((uint)lattice.GetFlatIndex(indexX, indexY + 1, indexZ));
                        }

                        if (indexZ < lattice.ResolutionZ - 1)
                        {
                            lineIndices.Add(currentIndex);
                            lineIndices.Add((uint)lattice.GetFlatIndex(indexX, indexY, indexZ + 1));
                        }
                    }
                }
            }

            var lineMesh = new Mesh([.. lineVertices], [.. lineIndices])
            {
                Topology = MeshTopology.Lines
            };

            _latticeNode = new MeshNode
            {
                Mesh = lineMesh,
                Color = new Vector3(0.6f, 0.6f, 0.6f),
                IsVisible = SelectedMode == DeformationMode.Ffd,
                IgnoreDepth = false,
                ForceWireframe = true
            };

            _activeMeshNode.AddChild(_latticeNode);
            _activeMeshNode.ApplyDeformers();
        }

        public void SubdivideActiveMesh()
        {
            if (_activeMeshNode?.Mesh is null)
            {
                return;
            }

            if (HasUnbakedChanges())
            {
                var shouldProceed = RequestConfirmation?.Invoke("Subdividing the mesh will discard the current unbaked deformation. Do you wish to proceed?");

                if (shouldProceed != true)
                {
                    return;
                }

                RestoreParameters();
            }

            var newMesh = _activeMeshNode.Mesh.Subdivide();
            ClearFfdState();

            _activeMeshNode.Mesh = newMesh;
            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);

            if (SelectedMode == DeformationMode.Ffd)
            {
                SetupFfdLattice();
            }

            ApplySelectedModeState();
        }

        public void RestoreParameters()
        {
            if (_activeMeshNode is null)
            {
                return;
            }

            _activeMeshNode.Translation = Vector3.Zero;
            _activeMeshNode.Rotation = Quaternion.Identity;
            _activeMeshNode.Scale = Vector3.One;

            Deformers.TwistAngle = 0f;
            Deformers.BendAngle = 0f;
            Deformers.TwistPivot = 0.5f;
            Deformers.BendPivot = 0.5f;

            if (SelectedMode == DeformationMode.Ffd)
            {
                ResetFfdLattice();
            }

            if (_activeMeshNode.Mesh?.Skinning is { } skinning)
            {
                skinning.Skeleton.ResetToBindPose();
                SyncBoneNodesToSkeleton();
                Deformers.IsLbsEnabled = SelectedMode == DeformationMode.LinearBlendSkinning;
            }

            _activeMeshNode.ApplyDeformers();
        }

        public void BakeTransformations()
        {
            if (_activeMeshNode is null)
            {
                return;
            }

            _activeMeshNode.ProcessPendingDeformations();

            if (_activeMeshNode.DeformedMesh is null)
            {
                return;
            }

            var currentDeformed = _activeMeshNode.DeformedMesh;
            var worldMatrix = _activeMeshNode.LocalTransform;
            var normalMatrix = new Matrix4(worldMatrix.Row0, worldMatrix.Row1, worldMatrix.Row2, worldMatrix.Row3);

            normalMatrix.Invert();
            normalMatrix.Transpose();

            var newVertices = new Vertex[currentDeformed.Vertices.Length];

            for (var index = 0; index < currentDeformed.Vertices.Length; index++)
            {
                var vertex = currentDeformed.Vertices[index];

                var transformedPosition = worldMatrix.TransformPoint(vertex.Position);
                var transformedNormal = vertex.Normal.LengthSquared > MathConstants.LengthTolerance
                    ? normalMatrix.TransformDirection(vertex.Normal).Normalized()
                    : vertex.Normal;

                newVertices[index] = new Vertex(transformedPosition, transformedNormal, vertex.TexCoords);
            }

            var newIndices = new uint[currentDeformed.Indices.Length];
            currentDeformed.Indices.CopyTo(newIndices, 0);

            var bakedMesh = new Mesh(newVertices, newIndices)
            {
                Topology = currentDeformed.Topology,
                Skinning = currentDeformed.Skinning
            };

            if (_activeMeshNode.Mesh?.Skinning is { } skinning)
            {
                var skeleton = skinning.Skeleton;
                skeleton.UpdateWorldTransforms();

                var boneVertices = new Vertex[skeleton.Bones.Count * 4];
                var epsilon = 0.01f;

                for (var index = 0; index < skeleton.Bones.Count; index++)
                {
                    var bone = skeleton.Bones[index];
                    var wt = bone.WorldTransform;
                    var pos = wt.ExtractTranslation();
                    var xAxis = wt.Row0.Xyz;
                    var yAxis = wt.Row1.Xyz;
                    var zAxis = wt.Row2.Xyz;

                    boneVertices[index * 4 + 0] = new Vertex(pos);
                    boneVertices[index * 4 + 1] = new Vertex(pos + xAxis * epsilon);
                    boneVertices[index * 4 + 2] = new Vertex(pos + yAxis * epsilon);
                    boneVertices[index * 4 + 3] = new Vertex(pos + zAxis * epsilon);
                }

                _activeMeshNode.Mesh.CalculateBounds(out var min, out var max);

                Deformers.TwistDeformer.Deform(boneVertices, min, max);
                Deformers.BendDeformer.Deform(boneVertices, min, max);
                Deformers.FfdDeformer.Deform(boneVertices);

                var newWorldTransforms = new Matrix4[skeleton.Bones.Count];

                for (var index = 0; index < skeleton.Bones.Count; index++)
                {
                    var p0 = boneVertices[index * 4 + 0].Position;
                    var p1 = boneVertices[index * 4 + 1].Position;
                    var p2 = boneVertices[index * 4 + 2].Position;
                    var p3 = boneVertices[index * 4 + 3].Position;

                    var newPos = worldMatrix.TransformPoint(p0);
                    var newX = worldMatrix.TransformPoint(p1) - newPos;
                    var newY = worldMatrix.TransformPoint(p2) - newPos;
                    var newZ = worldMatrix.TransformPoint(p3) - newPos;

                    var scaleX = newX.Length / epsilon;
                    var scaleY = newY.Length / epsilon;
                    var scaleZ = newZ.Length / epsilon;

                    newX.Normalize();
                    newY = (newY - newX * Vector3.Dot(newY, newX)).Normalized();
                    newZ = Vector3.Cross(newX, newY).Normalized();

                    newX *= scaleX;
                    newY *= scaleY;
                    newZ *= scaleZ;

                    var newWt = new Matrix4(
                        new Vector4(newX, 0f),
                        new Vector4(newY, 0f),
                        new Vector4(newZ, 0f),
                        new Vector4(newPos, 1f)
                    );

                    newWorldTransforms[index] = newWt;
                }

                for (var index = 0; index < skeleton.Bones.Count; index++)
                {
                    var bone = skeleton.Bones[index];

                    if (bone.ParentIndex is int parentIndex)
                    {
                        var parentWt = newWorldTransforms[parentIndex];
                        var invParentWt = parentWt.Inverted();
                        bone.LocalTransform = newWorldTransforms[index] * invParentWt;
                    }
                    else
                    {
                        bone.LocalTransform = newWorldTransforms[index];
                    }
                }
            }

            _activeMeshNode.Translation = Vector3.Zero;
            _activeMeshNode.Rotation = Quaternion.Identity;
            _activeMeshNode.Scale = Vector3.One;

            Deformers.TwistAngle = 0f;
            Deformers.BendAngle = 0f;
            ClearFfdState();

            if (_activeMeshNode.Mesh?.Skinning is { } updatedSkinning)
            {
                updatedSkinning.Skeleton.RebindToCurrentPose();
                SyncBoneNodesToSkeleton();
                Deformers.IsLbsEnabled = SelectedMode == DeformationMode.LinearBlendSkinning;
            }

            _activeMeshNode.Mesh = bakedMesh;
            CameraSystem.TargetSphere = BoundingSphere.FromAxisAlignedBoundingBox(_activeMeshNode.BoundingBox);

            if (SelectedMode == DeformationMode.Ffd)
            {
                SetupFfdLattice();
            }

            ApplySelectedModeState();
        }

        #endregion

        #region Private Logic

        private void ApplySelectedModeState()
        {
            GizmoSystem.IsEnabled = _hasModel && (SelectedMode == DeformationMode.Basic || SelectedMode == DeformationMode.Ffd || SelectedMode == DeformationMode.LinearBlendSkinning);
            SetFfdVisualsVisible(SelectedMode == DeformationMode.Ffd);
            SetSkeletonVisualsVisible(SelectedMode == DeformationMode.LinearBlendSkinning);
            Deformers.IsLbsEnabled = SelectedMode == DeformationMode.LinearBlendSkinning;

            if (!_hasModel)
            {
                GizmoSystem.TargetNode = null;
                return;
            }

            if (SelectedMode == DeformationMode.Basic)
            {
                GizmoSystem.TargetNode = _activeMeshNode;
            }
            else if (SelectedMode == DeformationMode.Ffd)
            {
                if (!Deformers.FfdDeformer.IsInitialized)
                {
                    SetupFfdLattice();
                }

                Gizmo.Mode = GizmoMode.Translate;
                GizmoSystem.TargetNode = null;
                SetFfdVisualsVisible(true);
            }
            else if (SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                Gizmo.Mode = GizmoMode.Rotate;
                GizmoSystem.TargetNode = _boneNodes.FirstOrDefault();
                SetSkeletonVisualsVisible(true);
            }
            else
            {
                GizmoSystem.TargetNode = null;
            }
        }

        private void OnControlPointMoved(int indexX, int indexY, int indexZ, Vector3 newPosition)
        {
            var lattice = Deformers.FfdDeformer.Lattice;

            if (lattice is null)
            {
                return;
            }

            Deformers.FfdDeformer.UpdateControlPoint(indexX, indexY, indexZ, newPosition);

            if (_latticeNode?.Mesh is not null)
            {
                var flatIndex = lattice.GetFlatIndex(indexX, indexY, indexZ);
                _latticeNode.Mesh.Vertices[flatIndex].Position = newPosition;
                _latticeNode.ApplyDeformers();
            }

            _activeMeshNode?.ApplyDeformers();
        }

        private void ResetFfdLattice()
        {
            if (!Deformers.FfdDeformer.IsInitialized)
            {
                if (SelectedMode == DeformationMode.Ffd)
                {
                    SetupFfdLattice();
                }

                return;
            }

            Deformers.FfdDeformer.Reset();
            UpdateFfdVisualsFromLattice();
        }

        private void UpdateFfdVisualsFromLattice()
        {
            var lattice = Deformers.FfdDeformer.Lattice;

            if (lattice is null)
            {
                return;
            }

            foreach (var controlPointNode in _ffdControlPoints)
            {
                var position = lattice.GetControlPoint(controlPointNode.IndexX, controlPointNode.IndexY, controlPointNode.IndexZ);
                controlPointNode.SetPositionFromLattice(position);
            }

            if (_latticeNode?.Mesh is null)
            {
                return;
            }

            for (var indexX = 0; indexX < lattice.ResolutionX; indexX++)
            {
                for (var indexY = 0; indexY < lattice.ResolutionY; indexY++)
                {
                    for (var indexZ = 0; indexZ < lattice.ResolutionZ; indexZ++)
                    {
                        var flatIndex = lattice.GetFlatIndex(indexX, indexY, indexZ);
                        _latticeNode.Mesh.Vertices[flatIndex].Position = lattice.GetControlPoint(indexX, indexY, indexZ);
                    }
                }
            }

            _latticeNode.ApplyDeformers();
        }

        private void SetFfdVisualsVisible(bool isVisible)
        {
            foreach (var controlPointNode in _ffdControlPoints)
            {
                controlPointNode.IsVisible = isVisible;
            }

            if (_latticeNode is not null)
            {
                _latticeNode.IsVisible = isVisible;
            }

            if (!isVisible && GizmoSystem.TargetNode is ControlPointNode)
            {
                GizmoSystem.TargetNode = null;
            }
        }

        private void SetupSkeletonVisuals(Mesh mesh)
        {
            var skinning = mesh.Skinning;

            if (skinning is null || _activeMeshNode is null)
            {
                return;
            }

            var radius = MathF.Max(0.01f, CameraSystem.TargetSphere.Radius * 0.02f);
            var jointMesh = MeshFactory.CreateSphere(radius, rings: 8, segments: 12, Vector3.Zero);
            GizmoSystem.BoneGizmoRadius = radius;
            var lineVertices = new List<Vertex>();
            var lineIndices = new List<uint>();

            var nodesByBoneIndex = new Dictionary<int, BoneNode>();

            foreach (var bone in skinning.Skeleton.Bones)
            {
                var boneNode = new BoneNode(bone)
                {
                    Mesh = jointMesh,
                    Color = new Vector3(1f, 0.85f, 0.2f),
                    IsVisible = SelectedMode == DeformationMode.LinearBlendSkinning,
                    IgnoreDepth = false,
                    ForceSolid = true
                };

                nodesByBoneIndex.Add(bone.Index, boneNode);
                _boneNodes.Add(boneNode);
            }

            foreach (var boneNode in _boneNodes)
            {
                if (boneNode.Bone.ParentIndex is int parentIndex && nodesByBoneIndex.TryGetValue(parentIndex, out var parentNode))
                {
                    parentNode.AddChild(boneNode);
                }
                else
                {
                    _activeMeshNode.AddChild(boneNode);
                }
            }

            skinning.Skeleton.UpdateWorldTransforms();

            foreach (var bone in skinning.Skeleton.Bones)
            {
                if (bone.ParentIndex is not int parentIndex)
                {
                    continue;
                }

                var parentPosition = skinning.Skeleton.Bones[parentIndex].WorldTransform.ExtractTranslation();
                var childPosition = bone.WorldTransform.ExtractTranslation();
                var vertexIndex = (uint)lineVertices.Count;

                lineVertices.Add(new Vertex(parentPosition));
                lineVertices.Add(new Vertex(childPosition));
                lineIndices.Add(vertexIndex);
                lineIndices.Add(vertexIndex + 1);
            }

            if (lineVertices.Count == 0)
            {
                return;
            }

            _skeletonLineNode = new MeshNode
            {
                Mesh = new Mesh([.. lineVertices], [.. lineIndices])
                {
                    Topology = MeshTopology.Lines
                },
                Color = new Vector3(0.15f, 0.9f, 1f),
                IsVisible = SelectedMode == DeformationMode.LinearBlendSkinning,
                ForceWireframe = true
            };

            _activeMeshNode.AddChild(_skeletonLineNode);
        }

        private void SyncBoneNodesToSkeleton()
        {
            foreach (var boneNode in _boneNodes)
            {
                boneNode.ApplyBoneTransform();
            }
        }

        private void UpdateSkeletonLinesFromBones()
        {
            if (_activeMeshNode?.Mesh?.Skinning is not { } skinning || _skeletonLineNode?.Mesh is not { } lineMesh)
            {
                return;
            }

            skinning.Skeleton.UpdateWorldTransforms();

            var lineVertexIndex = 0;

            foreach (var bone in skinning.Skeleton.Bones)
            {
                if (bone.ParentIndex is not int parentIndex || lineVertexIndex + 1 >= lineMesh.Vertices.Length)
                {
                    continue;
                }

                lineMesh.Vertices[lineVertexIndex++].Position = skinning.Skeleton.Bones[parentIndex].WorldTransform.ExtractTranslation();
                lineMesh.Vertices[lineVertexIndex++].Position = bone.WorldTransform.ExtractTranslation();
            }

            _skeletonLineNode.ApplyDeformers();
        }

        private void SetSkeletonVisualsVisible(bool isVisible)
        {
            foreach (var boneNode in _boneNodes)
            {
                boneNode.IsVisible = isVisible;
            }

            if (_skeletonLineNode is not null)
            {
                _skeletonLineNode.IsVisible = isVisible;
            }

            if (!isVisible && GizmoSystem.TargetNode is BoneNode)
            {
                GizmoSystem.TargetNode = null;
            }
        }

        private void ClearSkeletonVisuals()
        {
            foreach (var boneNode in _boneNodes)
            {
                boneNode.Parent?.RemoveChild(boneNode);
            }

            _boneNodes.Clear();

            if (SelectedMode == DeformationMode.Basic && _activeMeshNode is not null)
            {
                GizmoSystem.TargetNode = _activeMeshNode;
            }

            GizmoSystem.BoneGizmoRadius = 0f;
            _skeletonLineNode?.Parent?.RemoveChild(_skeletonLineNode);
            _skeletonLineNode = null;

            if (GizmoSystem.TargetNode is BoneNode)
            {
                GizmoSystem.TargetNode = null;
            }
        }

        private void ClearFfdState()
        {
            ClearFfdVisuals();
            Deformers.FfdDeformer.Clear();
        }

        private void ClearFfdVisuals()
        {
            foreach (var controlPointNode in _ffdControlPoints)
            {
                controlPointNode.Parent?.RemoveChild(controlPointNode);
            }

            _ffdControlPoints.Clear();

            _latticeNode?.Parent?.RemoveChild(_latticeNode);
            _latticeNode = null;

            if (GizmoSystem.TargetNode is ControlPointNode)
            {
                GizmoSystem.TargetNode = null;
            }
        }

        private bool HasUnbakedChanges()
        {
            if (_activeMeshNode is null)
            {
                return false;
            }

            if (SelectedMode == DeformationMode.Basic)
            {
                return _activeMeshNode.Translation != Vector3.Zero ||
                       _activeMeshNode.Rotation != Quaternion.Identity ||
                       _activeMeshNode.Scale != Vector3.One;
            }

            if (SelectedMode == DeformationMode.Twist)
            {
                return MathF.Abs(Deformers.TwistAngle) > MathConstants.ZeroTolerance;
            }

            if (SelectedMode == DeformationMode.Bend)
            {
                return MathF.Abs(Deformers.BendAngle) > MathConstants.ZeroTolerance;
            }

            if (SelectedMode == DeformationMode.Ffd)
            {
                return Deformers.FfdDeformer.HasChanges;
            }

            if (SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                return Deformers.LbsDeformer.IsEnabled && HasSkeletonChanges();
            }

            return false;
        }

        private bool HasSkeletonChanges()
        {
            if (_activeMeshNode?.Mesh?.Skinning is not { } skinning)
            {
                return false;
            }

            return skinning.Skeleton.Bones.Any(bone => !AreMatricesClose(bone.LocalTransform, bone.BindLocalTransform));
        }

        private void WarnIfMissingSkinning(string extension, bool hasSkinning)
        {
            if (hasSkinning || !SkinningExtensions.Contains(extension))
            {
                return;
            }

            RequestWarning?.Invoke("The loaded model does not contain skeleton or skinning data. Linear Blend Skinning is unavailable for this file.");
        }

        private static bool AreMatricesClose(Matrix4 left, Matrix4 right)
        {
            return
                (left.Row0 - right.Row0).LengthSquared <= MathConstants.ZeroTolerance &&
                (left.Row1 - right.Row1).LengthSquared <= MathConstants.ZeroTolerance &&
                (left.Row2 - right.Row2).LengthSquared <= MathConstants.ZeroTolerance &&
                (left.Row3 - right.Row3).LengthSquared <= MathConstants.ZeroTolerance;
        }

        #endregion
    }
}