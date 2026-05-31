using Application.Core.Abstractions;
using Deformation.Abstractions.Constants;
using Deformation.Abstractions.Enums;

namespace Application.UI.ViewModels
{
    public sealed class FfdDeformerViewModel(IWorkspaceSession session, IDialogService dialogService) : ViewModelBase, IDeformationPanelViewModel
    {
        #region Fields

        private int _ffdResolutionX = 3;
        private int _ffdResolutionY = 3;
        private int _ffdResolutionZ = 3;

        #endregion

        #region Properties

        public DeformationMode Mode => DeformationMode.Ffd;
        public static int MinimumFfdResolution => DeformationConstants.MinimumFfdResolution;
        public static int MaximumFfdResolution => DeformationConstants.MaximumFfdResolution;

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

        public void ResetToDefaults() { }

        public void OnActivated() { }

        public void ApplyMode(IWorkspaceSession workspaceSession)
        {
            workspaceSession.SetMode(Mode, FfdResolutionX, FfdResolutionY, FfdResolutionZ);
        }

        public void BakeTransformations(IWorkspaceSession workspaceSession)
        {
            workspaceSession.BakeTransformations(FfdResolutionX, FfdResolutionY, FfdResolutionZ);
        }

        public void SetupFfdLattice()
        {
            if (session.Scene.ActiveMeshNode is not null)
            {
                session.Deformations.SetupFfdLattice(session.Scene.ActiveMeshNode, FfdResolutionX, FfdResolutionY, FfdResolutionZ, session.Scene.CameraSystem.TargetSphere.Radius, true);
                session.Scene.GizmoSystem.Mode = GizmoMode.Translate;
            }
        }

        public void SubdivideActiveMesh()
        {
            if (!session.HasModel || session.Scene.ActiveMeshNode is null)
            {
                return;
            }

            if (session.Deformations.HasUnbakedChanges(session.Scene.ActiveMeshNode, Mode))
            {
                var shouldProceed = dialogService.ShowConfirmation("Subdividing the mesh will discard the current unbaked deformation. Do you wish to proceed?");

                if (!shouldProceed)
                {
                    return;
                }

                session.RestoreParameters();
            }

            session.SubdivideActiveMesh(FfdResolutionX, FfdResolutionY, FfdResolutionZ);
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