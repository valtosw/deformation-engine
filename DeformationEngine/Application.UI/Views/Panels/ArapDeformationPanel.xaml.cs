using Application.UI.ViewModels;
using System.Windows;

namespace Application.UI.Views.Panels
{
    public sealed partial class ArapDeformationPanel
    {
        public ArapDeformationPanel()
        {
            InitializeComponent();
        }

        private void ControlPoints_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            if (DataContext is ArapDeformerViewModel viewModel)
            {
                viewModel.ActivateControlPointMode();
            }
        }

        private void AnchorPoints_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            if (DataContext is ArapDeformerViewModel viewModel)
            {
                viewModel.ActivateAnchorPointMode();
            }
        }

        private void Deform_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            if (DataContext is ArapDeformerViewModel viewModel)
            {
                viewModel.ActivateDeformMode();
            }
        }
    }
}
