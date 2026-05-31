using Deformation.Abstractions.Enums;
using Deformation.Scene.Abstractions;

namespace Application.UI.ViewModels
{
    public sealed class GizmoViewModel(IGizmoSystem gizmoSystem) : ViewModelBase
    {
        #region Properties

        public static IEnumerable<GizmoMode> AvailableModes => Enum.GetValues<GizmoMode>();

        public GizmoMode Mode
        {
            get
            {
                return gizmoSystem.Mode;
            }
            set
            {
                if (gizmoSystem.Mode != value)
                {
                    gizmoSystem.Mode = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Public Logic

        public void Refresh()
        {
            OnPropertyChanged(nameof(Mode));
        }

        #endregion
    }
}