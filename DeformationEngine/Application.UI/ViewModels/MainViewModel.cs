using Application.Core.Abstractions;
using Deformation.Abstractions.Enums;
using Deformation.Interaction.Input;
using Rendering.Abstractions;
using System.Windows;

namespace Application.UI.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        #region Fields

        private readonly IWorkspaceSession _session;
        private DeformationMode _selectedMode = DeformationMode.Basic;

        #endregion

        #region Constructors

        public MainViewModel(IWorkspaceSession session)
        {
            _session = session;
            _session.StateChanged += OnSessionStateChanged;
            _session.WarningRequested = message => RequestWarning?.Invoke(message);

            Deformers = new DeformerViewModel(_session);
            Gizmo = new GizmoViewModel(_session.Scene.GizmoSystem);
        }

        #endregion

        #region Properties

        public DeformerViewModel Deformers { get; }
        public GizmoViewModel Gizmo { get; }

        public Func<string, bool>? RequestConfirmation { get; set; }
        public Action<string>? RequestWarning { get; set; }

        public bool HasModel => _session.HasModel;
        public bool HasSkinning => _session.HasSkinning;

        public DeformationMode SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (_selectedMode == value)
                {
                    return;
                }

                if (value == DeformationMode.LinearBlendSkinning && !HasSkinning)
                {
                    OnPropertyChanged();
                    return;
                }

                if (_session.Scene.ActiveMeshNode is not null &&
                    _session.Deformations.HasUnbakedChanges(_session.Scene.ActiveMeshNode, _selectedMode))
                {
                    var shouldProceed = RequestConfirmation?.Invoke("You haven't baked the deformation. Switching deformation type will discard the current unbaked changes. Do you wish to proceed?");

                    if (shouldProceed != true)
                    {
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            OnPropertyChanged(nameof(SelectedMode));
                        }));

                        return;
                    }

                    RestoreParameters();
                }

                _selectedMode = value;
                _session.SetMode(value, Deformers.FfdResolutionX, Deformers.FfdResolutionY, Deformers.FfdResolutionZ);

                Deformers.RefreshIsLbsEnabled();
                Gizmo.Refresh();

                OnPropertyChanged();
                OnPropertyChanged(nameof(BasicPanelVisibility));
                OnPropertyChanged(nameof(TwistPanelVisibility));
                OnPropertyChanged(nameof(BendPanelVisibility));
                OnPropertyChanged(nameof(FfdPanelVisibility));
                OnPropertyChanged(nameof(LbsPanelVisibility));
            }
        }

        public Visibility BasicPanelVisibility => SelectedMode == DeformationMode.Basic ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TwistPanelVisibility => SelectedMode == DeformationMode.Twist ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BendPanelVisibility => SelectedMode == DeformationMode.Bend ? Visibility.Visible : Visibility.Collapsed;
        public Visibility FfdPanelVisibility => SelectedMode == DeformationMode.Ffd ? Visibility.Visible : Visibility.Collapsed;
        public Visibility LbsPanelVisibility => SelectedMode == DeformationMode.LinearBlendSkinning ? Visibility.Visible : Visibility.Collapsed;

        public IEnumerable<DeformationMode> AvailableModes => Enum.GetValues<DeformationMode>().Where(mode => mode != DeformationMode.LinearBlendSkinning || HasSkinning);

        #endregion

        #region Public Logic

        public void InitializeRendering(IRenderingContext renderingContext) => _session.Scene.InitializeRendering(renderingContext);

        public void Resize(int width, int height) => _session.Scene.Resize(width, height);

        public void Render(float deltaTime)
        {
            _session.Scene.Render(deltaTime);

            if (SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                _session.Deformations.ApplyDeformations(_session.Scene.ActiveMeshNode);
            }
        }

        public void ProcessInput(IInputEvent inputEvent) => _session.Scene.ProcessInput(inputEvent);

        public void LoadMesh(string filePath)
        {
            _session.LoadMesh(filePath);

            if (!HasSkinning && SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                SelectedMode = DeformationMode.Basic;
            }
            else
            {
                _session.SetMode(SelectedMode, Deformers.FfdResolutionX, Deformers.FfdResolutionY, Deformers.FfdResolutionZ);
            }
        }

        public void SetupFfdLattice()
        {
            if (_session.Scene.ActiveMeshNode is not null)
            {
                _session.Deformations.SetupFfdLattice(_session.Scene.ActiveMeshNode, Deformers.FfdResolutionX, Deformers.FfdResolutionY, Deformers.FfdResolutionZ, _session.Scene.CameraSystem.TargetSphere.Radius, true);
                Gizmo.Mode = GizmoMode.Translate;
            }
        }

        public void SubdivideActiveMesh()
        {
            if (!HasModel || _session.Scene.ActiveMeshNode is null)
            {
                return;
            }

            if (_session.Deformations.HasUnbakedChanges(_session.Scene.ActiveMeshNode, SelectedMode))
            {
                var shouldProceed = RequestConfirmation?.Invoke("Subdividing the mesh will discard the current unbaked deformation. Do you wish to proceed?");

                if (shouldProceed != true)
                {
                    return;
                }

                RestoreParameters();
            }

            _session.SubdivideActiveMesh(Deformers.FfdResolutionX, Deformers.FfdResolutionY, Deformers.FfdResolutionZ);
        }

        public void RestoreParameters()
        {
            if (!HasModel)
            {
                return;
            }

            Deformers.ResetToDefaults();
            _session.RestoreParameters();
            Deformers.RefreshIsLbsEnabled();
        }

        public void BakeTransformations()
        {
            if (!HasModel)
            {
                return;
            }

            _session.BakeTransformations(Deformers.FfdResolutionX, Deformers.FfdResolutionY, Deformers.FfdResolutionZ);
            Deformers.ResetToDefaults();
            Deformers.RefreshIsLbsEnabled();
        }

        #endregion

        #region Private Logic

        private void OnSessionStateChanged(object? sender, EventArgs eventArgs)
        {
            OnPropertyChanged(nameof(HasModel));
            OnPropertyChanged(nameof(HasSkinning));
            OnPropertyChanged(nameof(AvailableModes));
        }

        #endregion
    }
}