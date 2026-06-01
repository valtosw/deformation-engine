using Application.Core.Abstractions;
using Deformation.Abstractions.Enums;
using Deformation.Modifiers.Deformers;
using Deformation.Scene.Abstractions;
using OpenTK.Mathematics;

namespace Application.UI.ViewModels
{
    public sealed class ArapDeformerViewModel(
        IWorkspaceSession session,
        IArapSelectionVisualBuilder arapSelectionBuilder,
        GizmoViewModel gizmo) : ViewModelBase, IDeformationPanelViewModel
    {
        #region Fields

        private readonly ArapDeformer _deformer = session.Deformations.GetDeformer<ArapDeformer>();

        #endregion

        #region Properties

        public DeformationMode Mode => DeformationMode.AsRigidAsPossible;
        public GizmoViewModel Gizmo { get; } = gizmo;
        public static IEnumerable<GizmoMode> AvailableGizmoModes => [GizmoMode.Translate, GizmoMode.Rotate];

        public bool IsManualAnchorSelected
        {
            get => _deformer.AnchorType == ArapAnchorType.Manual;
            set
            {
                if (value)
                {
                    SetAnchorType(ArapAnchorType.Manual);
                }
            }
        }

        public bool IsDistanceAnchorSelected
        {
            get => _deformer.AnchorType == ArapAnchorType.Distance;
            set
            {
                if (value)
                {
                    SetAnchorType(ArapAnchorType.Distance);
                }
            }
        }

        public bool IsDistanceSliderEnabled => _deformer.AnchorType == ArapAnchorType.Distance;
        public bool IsManualAnchorPaintEnabled => _deformer.AnchorType == ArapAnchorType.Manual;

        public float AnchorDistance
        {
            get => _deformer.AnchorDistance;
            set
            {
                _deformer.SetAnchorDistance(value);
                session.Scene.ActiveMeshNode?.ApplyDeformers();
                OnPropertyChanged();
            }
        }

        public int Iterations
        {
            get => _deformer.Iterations;
            set
            {
                _deformer.SetIterations(value);
                session.Scene.ActiveMeshNode?.ApplyDeformers();
                OnPropertyChanged();
            }
        }

        public ArapActionMode ActionMode
        {
            get => _deformer.ActionMode;
            private set
            {
                if (_deformer.ActionMode == value)
                {
                    return;
                }

                _deformer.SetActionMode(value);
                ConfigureInteractionMode();
                session.Scene.ActiveMeshNode?.ApplyDeformers();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsControlPointMode));
                OnPropertyChanged(nameof(IsAnchorPointMode));
                OnPropertyChanged(nameof(IsDeformMode));
            }
        }

        public bool IsControlPointMode => _deformer.ActionMode == ArapActionMode.ControlPoints;
        public bool IsAnchorPointMode => _deformer.ActionMode == ArapActionMode.AnchorPoints;
        public bool IsDeformMode => _deformer.ActionMode == ArapActionMode.Deform;

        #endregion

        #region Public Logic

        public void ResetToDefaults()
        {
            _deformer.SetAnchorType(ArapAnchorType.Manual);
            _deformer.SetAnchorDistance(0.25f);
            _deformer.SetIterations(12);
            _deformer.SetActionMode(ArapActionMode.ControlPoints);
        }

        public void OnActivated()
        {
            ConfigureInteractionMode();
            Gizmo.Refresh();
            RefreshAllProperties();
        }

        public void ApplyMode(IWorkspaceSession workspaceSession)
        {
            workspaceSession.SetMode(Mode, 3, 3, 3);
            ConfigureInteractionMode();
        }

        public void BakeTransformations(IWorkspaceSession workspaceSession)
        {
            workspaceSession.BakeTransformations(3, 3, 3);
        }

        public void ActivateControlPointMode()
        {
            ActionMode = ArapActionMode.ControlPoints;
        }

        public void ActivateAnchorPointMode()
        {
            if (_deformer.AnchorType == ArapAnchorType.Manual)
            {
                ActionMode = ArapActionMode.AnchorPoints;
            }
        }

        public void ActivateDeformMode()
        {
            ActionMode = ArapActionMode.Deform;
        }

        #endregion

        #region Private Logic

        private void SetAnchorType(ArapAnchorType anchorType)
        {
            _deformer.SetAnchorType(anchorType);

            if (anchorType == ArapAnchorType.Distance && _deformer.ActionMode == ArapActionMode.AnchorPoints)
            {
                ActionMode = ArapActionMode.ControlPoints;
            }

            session.Scene.ActiveMeshNode?.ApplyDeformers();
            OnPropertyChanged(nameof(IsManualAnchorSelected));
            OnPropertyChanged(nameof(IsDistanceAnchorSelected));
            OnPropertyChanged(nameof(IsDistanceSliderEnabled));
            OnPropertyChanged(nameof(IsManualAnchorPaintEnabled));
        }

        private void ConfigureInteractionMode()
        {
            if (session.CurrentMode != Mode)
            {
                return;
            }

            if (_deformer.ActionMode == ArapActionMode.Deform && _deformer.ControlVertices.Count > 0)
            {
                _deformer.BeginDeform();
                session.Scene.GizmoSystem.IsEnabled = true;
                session.Scene.GizmoSystem.TargetNode = arapSelectionBuilder.HandleNode;

                if (arapSelectionBuilder.HandleNode is not null)
                {
                    arapSelectionBuilder.HandleNode.SetPose(_deformer.Pivot, Quaternion.Identity);
                }
            }
            else
            {
                session.Scene.GizmoSystem.TargetNode = null;
                session.Scene.GizmoSystem.IsEnabled = false;
            }
        }

        private void RefreshAllProperties()
        {
            OnPropertyChanged(nameof(IsManualAnchorSelected));
            OnPropertyChanged(nameof(IsDistanceAnchorSelected));
            OnPropertyChanged(nameof(IsDistanceSliderEnabled));
            OnPropertyChanged(nameof(IsManualAnchorPaintEnabled));
            OnPropertyChanged(nameof(AnchorDistance));
            OnPropertyChanged(nameof(Iterations));
            OnPropertyChanged(nameof(ActionMode));
            OnPropertyChanged(nameof(IsControlPointMode));
            OnPropertyChanged(nameof(IsAnchorPointMode));
            OnPropertyChanged(nameof(IsDeformMode));
        }

        #endregion
    }

}
