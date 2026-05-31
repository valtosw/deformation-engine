using Application.Core.Abstractions;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Modifiers.Deformers;

namespace Application.UI.ViewModels
{
    public sealed class TwistDeformerViewModel(IWorkspaceSession session) : ViewModelBase, IDeformationPanelViewModel
    {
        #region Fields

        private float _twistAngle;
        private Axis _twistAxis = Axis.Y;
        private float _twistPivot = 0.5f;
        private bool _preventSelfIntersection = true;

        #endregion

        #region Properties

        public DeformationMode Mode => DeformationMode.Twist;
        public static IEnumerable<Axis> AvailableAxes => Enum.GetValues<Axis>();
        public static float MinimumTwistAngle => DeformationConstants.MinimumTwistAngle;
        public static float MaximumTwistAngle => DeformationConstants.MaximumTwistAngle;

        public bool PreventSelfIntersection
        {
            get => _preventSelfIntersection;
            set
            {
                if (SetProperty(ref _preventSelfIntersection, value))
                {
                    session.Deformations.GetDeformer<TwistDeformer>().PreventSelfIntersection = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        public float TwistAngle
        {
            get => _twistAngle;
            set
            {
                if (SetProperty(ref _twistAngle, value))
                {
                    session.Deformations.GetDeformer<TwistDeformer>().Angle = value;
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
                    session.Deformations.GetDeformer<TwistDeformer>().Axis = value;
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
                    session.Deformations.GetDeformer<TwistDeformer>().Pivot = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        #endregion

        #region Public Logic

        public void ResetToDefaults()
        {
            TwistAngle = 0f;
            TwistPivot = 0.5f;
        }

        public void OnActivated() { }

        public void ApplyMode(IWorkspaceSession workspaceSession)
        {
            workspaceSession.SetMode(Mode, 3, 3, 3);
        }

        public void BakeTransformations(IWorkspaceSession workspaceSession)
        {
            workspaceSession.BakeTransformations(3, 3, 3);
        }

        #endregion
    }
}