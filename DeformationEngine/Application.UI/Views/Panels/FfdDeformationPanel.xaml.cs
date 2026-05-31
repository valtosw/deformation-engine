using System.Windows;
using Application.UI.ViewModels;

namespace Application.UI.Views.Panels
{
    public sealed partial class FfdDeformationPanel
    {
        public FfdDeformationPanel()
        {
            InitializeComponent();
        }

        private void GenerateLattice_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            if (DataContext is FfdDeformerViewModel viewModel)
            {
                viewModel.SetupFfdLattice();
            }
        }

        private void SubdivideMesh_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            if (DataContext is FfdDeformerViewModel viewModel)
            {
                viewModel.SubdivideActiveMesh();
            }
        }
    }
}