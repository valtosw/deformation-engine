using Deformation.Abstractions.Enums;

namespace Application.Core.Abstractions
{
    public interface IWorkspaceSession
    {
        ISceneDirector Scene { get; }
        IDeformationWorkflow Deformations { get; }

        bool HasModel { get; }
        bool HasSkinning { get; }
        DeformationMode CurrentMode { get; }

        Action<string>? WarningRequested { get; set; }
        event EventHandler? StateChanged;

        void LoadMesh(string filePath);
        void SetMode(DeformationMode mode, int resolutionX, int resolutionY, int resolutionZ);
        void SubdivideActiveMesh(int resolutionX, int resolutionY, int resolutionZ);
        void BakeTransformations(int resolutionX, int resolutionY, int resolutionZ);
        void RestoreParameters();
    }
}