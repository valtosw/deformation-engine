using Application.Core.Abstractions;
using Deformation.Abstractions.Enums;
using Deformation.Interaction.Input;
using Rendering.Abstractions;
using System.Windows.Input;

namespace Application.UI.ViewModels
{
    public sealed class MainViewModel : ViewModelBase, IDisposable
    {
        #region Fields

        private readonly IWorkspaceSession _session;
        private readonly IDialogService _dialogService;
        private readonly Dictionary<DeformationMode, IDeformationPanelViewModel> _panels;

        private DeformationMode _selectedMode = DeformationMode.Basic;
        private IDeformationPanelViewModel _currentDeformerViewModel;

        #endregion

        #region Constructors

        public MainViewModel(
            IWorkspaceSession session,
            IDialogService dialogService,
            IEnumerable<IDeformationPanelViewModel> panels)
        {
            _session = session;
            _dialogService = dialogService;
            _panels = panels.ToDictionary(panel => panel.Mode);
            _currentDeformerViewModel = _panels[_selectedMode];

            _session.StateChanged += OnSessionStateChanged;
            _session.WarningRequested = OnWarningRequested;
        }

        #endregion

        #region Properties

        public IDeformationPanelViewModel CurrentDeformerViewModel
        {
            get => _currentDeformerViewModel;
            private set => SetProperty(ref _currentDeformerViewModel, value);
        }

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
                    var shouldProceed = _dialogService.ShowConfirmation("You haven't baked the deformation. Switching deformation type will discard the current unbaked changes. Do you wish to proceed?");

                    if (!shouldProceed)
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
                CurrentDeformerViewModel = _panels[value];

                CurrentDeformerViewModel.ApplyMode(_session);
                CurrentDeformerViewModel.OnActivated();

                OnPropertyChanged();
            }
        }

        public IEnumerable<DeformationMode> AvailableModes => Enum.GetValues<DeformationMode>().Where(mode => mode != DeformationMode.LinearBlendSkinning || HasSkinning);

        #endregion

        #region Public Logic

        public void Dispose()
        {
            _session.StateChanged -= OnSessionStateChanged;
            _session.WarningRequested -= OnWarningRequested;
        }

        public void InitializeRendering(IRenderingContext renderingContext)
        {
            _session.Scene.InitializeRendering(renderingContext);
        }

        public void Resize(int width, int height)
        {
            _session.Scene.Resize(width, height);
        }

        public void Render(float deltaTime)
        {
            _session.Scene.Render(deltaTime);

            if (SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                _session.Deformations.ApplyDeformations(_session.Scene.ActiveMeshNode);
            }
        }

        public void ProcessInput(IInputEvent inputEvent)
        {
            _session.Scene.ProcessInput(inputEvent);
        }

        public Cursor GetViewportCursor(bool isEraseModifierPressed)
        {
            if (SelectedMode == DeformationMode.AsRigidAsPossible &&
                CurrentDeformerViewModel is ArapDeformerViewModel arapViewModel &&
                (arapViewModel.IsControlPointMode || arapViewModel.IsAnchorPointMode))
            {
                return isEraseModifierPressed ? Cursors.No : Cursors.Pen;
            }

            return Cursors.Arrow;
        }

        public void LoadMesh(string filePath)
        {
            _session.LoadMesh(filePath);

            if (!HasSkinning && SelectedMode == DeformationMode.LinearBlendSkinning)
            {
                SelectedMode = DeformationMode.Basic;
            }
            else
            {
                CurrentDeformerViewModel.ApplyMode(_session);
            }
        }

        public void RestoreParameters()
        {
            if (!HasModel)
            {
                return;
            }

            foreach (var panel in _panels.Values)
            {
                panel.ResetToDefaults();
            }

            _session.RestoreParameters();
            CurrentDeformerViewModel.OnActivated();
        }

        public void BakeTransformations()
        {
            if (!HasModel)
            {
                return;
            }

            CurrentDeformerViewModel.BakeTransformations(_session);

            foreach (var panel in _panels.Values)
            {
                panel.ResetToDefaults();
            }

            CurrentDeformerViewModel.OnActivated();
        }

        #endregion

        #region Private Logic

        private void OnSessionStateChanged(object? sender, EventArgs eventArgs)
        {
            OnPropertyChanged(nameof(HasModel));
            OnPropertyChanged(nameof(HasSkinning));
            OnPropertyChanged(nameof(AvailableModes));
        }

        private void OnWarningRequested(string message)
        {
            _dialogService.ShowWarning(message);
        }

        #endregion
    }
}
