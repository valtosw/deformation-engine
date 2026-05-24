using System.Windows;
using Application.UI.ViewModels;

namespace Application.UI.Controls
{
    public sealed partial class ParameterPanel
    {
        public ParameterPanel()
        {
            InitializeComponent();
        }

        private void BakeChanges_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.BakeTransformations();
            }
        }

        private void ResetDeformations_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.RestoreParameters();
            }
        }
    }
}
