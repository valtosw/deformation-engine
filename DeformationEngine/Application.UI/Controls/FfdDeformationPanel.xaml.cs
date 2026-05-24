using System.Windows;
using Application.UI.ViewModels;

namespace Application.UI.Controls
{
    public sealed partial class FfdDeformationPanel
    {
        public FfdDeformationPanel()
        {
            InitializeComponent();
        }

        private void GenerateLattice_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SetupFfdLattice();
            }
        }

        private void SubdivideMesh_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.SubdivideActiveMesh();
            }
        }
    }
}