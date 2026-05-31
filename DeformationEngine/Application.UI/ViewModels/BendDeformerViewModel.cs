using Application.Core.Abstractions;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;
using Deformation.Modifiers.Deformers;
using System;
using System.Collections.Generic;

namespace Application.UI.ViewModels
{
    public sealed class BendDeformerViewModel(IWorkspaceSession session) : ViewModelBase, IDeformationPanelViewModel
    {
        #region Fields

        private float _bendAngle;
        private Axis _bendPrimaryAxis = Axis.Y;
        private Axis _bendAxis = Axis.X;
        private float _bendPivot = 0.5f;
        private bool _preventSelfIntersection = true;

        #endregion

        #region Properties

        public DeformationMode Mode => DeformationMode.Bend;
        public static IEnumerable<Axis> AvailableAxes => Enum.GetValues<Axis>();
        public static float MinimumBendAngle => DeformationConstants.MinimumBendAngle;
        public static float MaximumBendAngle => DeformationConstants.MaximumBendAngle;

        public bool PreventSelfIntersection
        {
            get => _preventSelfIntersection;
            set
            {
                if (SetProperty(ref _preventSelfIntersection, value))
                {
                    session.Deformations.GetDeformer<BendDeformer>().PreventSelfIntersection = value;
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
                    session.Deformations.GetDeformer<BendDeformer>().Angle = value;
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
                    session.Deformations.GetDeformer<BendDeformer>().PrimaryAxis = value;
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
                    session.Deformations.GetDeformer<BendDeformer>().BendAxis = value;
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
                    session.Deformations.GetDeformer<BendDeformer>().Pivot = value;
                    session.Deformations.ApplyDeformations(session.Scene.ActiveMeshNode);
                }
            }
        }

        #endregion

        #region Public Logic

        public void ResetToDefaults()
        {
            BendAngle = 0f;
            BendPivot = 0.5f;
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