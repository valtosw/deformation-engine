using Application.Core.Abstractions;
using Deformation.Abstractions.Enums;

namespace Application.UI.ViewModels
{
    public sealed class BasicDeformationViewModel(GizmoViewModel gizmo) : ViewModelBase, IDeformationPanelViewModel
    {
        #region Properties

        public DeformationMode Mode => DeformationMode.Basic;
        public GizmoViewModel Gizmo { get; } = gizmo;

        #endregion

        #region Public Logic

        public void ResetToDefaults() { }

        public void OnActivated()
        {
            Gizmo.Refresh();
        }

        public void ApplyMode(IWorkspaceSession session)
        {
            session.SetMode(Mode, 3, 3, 3);
        }

        public void BakeTransformations(IWorkspaceSession session)
        {
            session.BakeTransformations(3, 3, 3);
        }

        #endregion
    }
}