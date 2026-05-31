using Application.Core.Abstractions;
using Deformation.Abstractions.Enums;
using Deformation.Modifiers.Deformers;

namespace Application.UI.ViewModels
{
    public sealed class LbsDeformerViewModel(IWorkspaceSession session) : ViewModelBase, IDeformationPanelViewModel
    {
        #region Properties

        public DeformationMode Mode => DeformationMode.LinearBlendSkinning;

        public bool IsLbsEnabled
        {
            get => session.Deformations.GetDeformer<LbsDeformer>().IsEnabled;
            set
            {
                if (session.Deformations.GetDeformer<LbsDeformer>().IsEnabled == value)
                {
                    return;
                }

                session.Deformations.SetLbsEnabled(value, session.Scene.ActiveMeshNode);
                OnPropertyChanged();
            }
        }

        #endregion

        #region Public Logic

        public void ResetToDefaults() { }

        public void OnActivated()
        {
            OnPropertyChanged(nameof(IsLbsEnabled));
        }

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