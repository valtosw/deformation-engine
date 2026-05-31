using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Nodes;

namespace Application.UI.ViewModels
{
    public sealed class DeformerViewModel : ViewModelBase
    {
        #region Fields

        private float _twistAngle;
        private Axis _twistAxis = Axis.Y;
        private float _twistPivot = 0.5f;

        private float _bendAngle;
        private Axis _bendPrimaryAxis = Axis.Y;
        private Axis _bendAxis = Axis.X;
        private float _bendPivot = 0.5f;

        private bool _preventSelfIntersection = true;
        private MeshNode? _activeMeshNode;

        private int _ffdResolutionX = 3;
        private int _ffdResolutionY = 3;
        private int _ffdResolutionZ = 3;

        #endregion

        #region Properties

        public TwistDeformer TwistDeformer { get; } = new();
        public BendDeformer BendDeformer { get; } = new();
        public FfdDeformer FfdDeformer { get; } = new();
        public LbsDeformer LbsDeformer { get; } = new();

        public static IEnumerable<Axis> AvailableAxes => Enum.GetValues<Axis>();

        public static float MinimumTwistAngle => DeformationConstants.MinimumTwistAngle;
        public static float MaximumTwistAngle => DeformationConstants.MaximumTwistAngle;

        public static float MinimumBendAngle => DeformationConstants.MinimumBendAngle;
        public static float MaximumBendAngle => DeformationConstants.MaximumBendAngle;

        public static int MinimumFfdResolution => DeformationConstants.MinimumFfdResolution;
        public static int MaximumFfdResolution => DeformationConstants.MaximumFfdResolution;

        public MeshNode? ActiveMeshNode
        {
            get
            {
                return _activeMeshNode;
            }
            set
            {
                _activeMeshNode = value;
                OnPropertyChanged();
            }
        }

        public bool PreventSelfIntersection
        {
            get
            {
                return _preventSelfIntersection;
            }
            set
            {
                if (SetProperty(ref _preventSelfIntersection, value))
                {
                    TwistDeformer.PreventSelfIntersection = value;
                    BendDeformer.PreventSelfIntersection = value;
                    ApplyDeformations();
                }
            }
        }

        public bool IsLbsEnabled
        {
            get
            {
                return LbsDeformer.IsEnabled;
            }
            set
            {
                if (LbsDeformer.IsEnabled == value)
                {
                    return;
                }

                LbsDeformer.IsEnabled = value;
                OnPropertyChanged();
                ApplyDeformations();
            }
        }

        public float TwistAngle
        {
            get
            {
                return _twistAngle;
            }
            set
            {
                if (SetProperty(ref _twistAngle, value))
                {
                    TwistDeformer.Angle = value;
                    ApplyDeformations();
                }
            }
        }

        public Axis TwistAxis
        {
            get
            {
                return _twistAxis;
            }
            set
            {
                if (SetProperty(ref _twistAxis, value))
                {
                    TwistDeformer.Axis = value;
                    ApplyDeformations();
                }
            }
        }

        public float TwistPivot
        {
            get
            {
                return _twistPivot;
            }
            set
            {
                if (SetProperty(ref _twistPivot, value))
                {
                    TwistDeformer.Pivot = value;
                    ApplyDeformations();
                }
            }
        }

        public float BendAngle
        {
            get
            {
                return _bendAngle;
            }
            set
            {
                if (SetProperty(ref _bendAngle, value))
                {
                    BendDeformer.Angle = value;
                    ApplyDeformations();
                }
            }
        }

        public Axis BendPrimaryAxis
        {
            get
            {
                return _bendPrimaryAxis;
            }
            set
            {
                if (SetProperty(ref _bendPrimaryAxis, value))
                {
                    BendDeformer.PrimaryAxis = value;
                    ApplyDeformations();
                }
            }
        }

        public Axis BendAxis
        {
            get
            {
                return _bendAxis;
            }
            set
            {
                if (SetProperty(ref _bendAxis, value))
                {
                    BendDeformer.BendAxis = value;
                    ApplyDeformations();
                }
            }
        }

        public float BendPivot
        {
            get
            {
                return _bendPivot;
            }
            set
            {
                if (SetProperty(ref _bendPivot, value))
                {
                    BendDeformer.Pivot = value;
                    ApplyDeformations();
                }
            }
        }

        public int FfdResolutionX
        {
            get
            {
                return _ffdResolutionX;
            }
            set
            {
                SetProperty(ref _ffdResolutionX, ClampFfdResolution(value));
            }
        }

        public int FfdResolutionY
        {
            get
            {
                return _ffdResolutionY;
            }
            set
            {
                SetProperty(ref _ffdResolutionY, ClampFfdResolution(value));
            }
        }

        public int FfdResolutionZ
        {
            get
            {
                return _ffdResolutionZ;
            }
            set
            {
                SetProperty(ref _ffdResolutionZ, ClampFfdResolution(value));
            }
        }

        #endregion

        #region Private Logic

        private void ApplyDeformations()
        {
            _activeMeshNode?.ApplyDeformers();
        }

        private static int ClampFfdResolution(int value)
        {
            return Math.Clamp(value, DeformationConstants.MinimumFfdResolution, DeformationConstants.MaximumFfdResolution);
        }

        #endregion
    }
}