using System;
using System.Collections.Generic;
using Deformation.Abstractions.Enums;
using Deformation.Interaction;

namespace Application.UI.ViewModels
{
    public sealed class GizmoViewModel(GizmoController controller) : ViewModelBase
    {

        #region Constructors

        #endregion

        #region Properties

        public bool IsEnabled
        {
            get
            {
                return controller.IsEnabled;
            }
            set
            {
                if (controller.IsEnabled != value)
                {
                    controller.IsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public GizmoMode Mode
        {
            get
            {
                return controller.Mode;
            }
            set
            {
                if (controller.Mode != value)
                {
                    controller.Mode = value;
                    OnPropertyChanged();
                }
            }
        }

        public static IEnumerable<GizmoMode> AvailableModes => Enum.GetValues<GizmoMode>();

        #endregion
    }
}