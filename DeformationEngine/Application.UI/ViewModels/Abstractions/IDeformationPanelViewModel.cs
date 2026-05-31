using Application.Core.Abstractions;
using Deformation.Abstractions.Enums;

namespace Application.UI.ViewModels
{
    public interface IDeformationPanelViewModel
    {
        DeformationMode Mode { get; }

        void ResetToDefaults();
        void OnActivated();
        void ApplyMode(IWorkspaceSession session);
        void BakeTransformations(IWorkspaceSession session);
    }
}