using Application.Core.Abstractions;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;

namespace Application.UI.ViewModels
{
    public sealed class DeformerViewModel(IWorkspaceSession session) : ViewModelBase
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

        private int _ffdResolutionX = 3;
        private int _ffdResolutionY = 3;
        private int _ffdResolutionZ = 3;

        #endregion

        #region Properties

        public static IEnumerable<Axis> AvailableAxes => Enum.GetValues<Axis>();

        public static float MinimumTwistAngle => DeformationConstants.MinimumTwistAngle;
        public static float MaximumTwistAngle => DeformationConstants.MaximumTwistAngle;

        public static float MinimumBendAngle => DeformationConstants.MinimumBendAngle;
        public static float MaximumBendAngle => DeformationConstants.MaximumBendAngle;

        public static int MinimumFfdResolution => DeformationConstants.MinimumFfdResolution;
        public static int MaximumFfdResolution => DeformationConstants.MaximumFfdResolution;

        public bool PreventSelfIntersection
        {
            get => _preventSelfIntersection;
            set
            {
                if (SetProperty(ref _preventSelfIntersection, value))
                {
                    session.Deformations.TwistDeformer.PreventSelfIntersection = value;
                    session.Deformations.BendDeformer.PreventSelfIntersection = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        public bool IsLbsEnabled
        {
            get => session.Deformations.LbsDeformer.IsEnabled;
            set
            {
                if (session.Deformations.LbsDeformer.IsEnabled == value)
                {
                    return;
                }

                session.Deformations.SetLbsEnabled(value, session.Scene.ActiveMeshNode);
                OnPropertyChanged();
            }
        }

        public float TwistAngle
        {
            get => _twistAngle;
            set
            {
                if (SetProperty(ref _twistAngle, value))
                {
                    session.Deformations.TwistDeformer.Angle = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        public Axis TwistAxis
        {
            get => _twistAxis;
            set
            {
                if (SetProperty(ref _twistAxis, value))
                {
                    session.Deformations.TwistDeformer.Axis = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        public float TwistPivot
        {
            get => _twistPivot;
            set
            {
                if (SetProperty(ref _twistPivot, value))
                {
                    session.Deformations.TwistDeformer.Pivot = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        public float BendAngle
        {
            get => _bendAngle;
            set
            {
                if (SetProperty(ref _bendAngle, value))
                {
                    session.Deformations.BendDeformer.Angle = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        public Axis BendPrimaryAxis
        {
            get => _bendPrimaryAxis;
            set
            {
                if (SetProperty(ref _bendPrimaryAxis, value))
                {
                    session.Deformations.BendDeformer.PrimaryAxis = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        public Axis BendAxis
        {
            get => _bendAxis;
            set
            {
                if (SetProperty(ref _bendAxis, value))
                {
                    session.Deformations.BendDeformer.BendAxis = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        public float BendPivot
        {
            get => _bendPivot;
            set
            {
                if (SetProperty(ref _bendPivot, value))
                {
                    session.Deformations.BendDeformer.Pivot = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        public int FfdResolutionX
        {
            get => _ffdResolutionX;
            set => SetProperty(ref _ffdResolutionX, ClampFfdResolution(value));
        }

        public int FfdResolutionY
        {
            get => _ffdResolutionY;
            set => SetProperty(ref _ffdResolutionY, ClampFfdResolution(value));
        }

        public int FfdResolutionZ
        {
            get => _ffdResolutionZ;
            set => SetProperty(ref _ffdResolutionZ, ClampFfdResolution(value));
        }

        #endregion

        #region Public Logic

        public void ResetToDefaults()
        {
            TwistAngle = 0f;
            BendAngle = 0f;
            TwistPivot = 0.5f;
            BendPivot = 0.5f;
        }

        public void RefreshIsLbsEnabled()
        {
            OnPropertyChanged(nameof(IsLbsEnabled));
        }

        #endregion

        #region Private Logic

        private static int ClampFfdResolution(int value)
        {
            return Math.Clamp(value, DeformationConstants.MinimumFfdResolution, DeformationConstants.MaximumFfdResolution);
        }

        #endregion
    }
}