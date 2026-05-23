using Application.UI.Extensions;
using Application.UI.ViewModels;
using Deformation.Interaction.Input;
using Microsoft.Win32;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using Rendering.OpenGL;
using System.Windows;
using System.Windows.Input;
using InputType = Deformation.Interaction.Input.InputType;
using Key = Deformation.Interaction.Input.Key;

namespace Application.UI.Windows
{
    public sealed partial class MainWindow
    {
        #region Constructors

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;

            GlRenderingControl.Start(new GLWpfControlSettings
            {
                MajorVersion = 3,
                MinorVersion = 3
            });
        }

        #endregion

        #region Properties

        public MainViewModel ViewModel { get; }

        #endregion

        #region Private Logic

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            GlRenderingControl.Dispose();
        }

        private void GlRenderingControl_OnReady()
        {
            var shader = new Shader("default.vert", "default.frag");
            var renderingContext = new GlRenderingContext(shader);

            ViewModel.InitializeRendering(renderingContext);
            ViewModel.Resize((int)GlRenderingControl.ActualWidth, (int)GlRenderingControl.ActualHeight);
        }

        private void GlRenderingControl_OnRender(TimeSpan delta)
        {
            ViewModel.Render((float)delta.TotalSeconds);
        }

        private void GlRenderingControl_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.Resize((int)e.NewSize.Width, (int)e.NewSize.Height);
        }

        private void GlRenderingControl_OnMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(GlRenderingControl);
            ViewModel.ProcessInput(new MouseMoveEvent(new Vector2((float)position.X, (float)position.Y)));
        }

        private void GlRenderingControl_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            GlRenderingControl.CaptureMouse();

            var position = e.GetPosition(GlRenderingControl);
            var button = e.ChangedButton.ToEngineMouseButton();

            ViewModel.ProcessInput(new MouseClickEvent(
                Position: new Vector2((float)position.X, (float)position.Y),
                Button: button,
                InputType: InputType.Down));
        }

        private void GlRenderingControl_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (GlRenderingControl.IsMouseCaptured)
            {
                GlRenderingControl.ReleaseMouseCapture();
            }

            var position = e.GetPosition(GlRenderingControl);
            var button = e.ChangedButton.ToEngineMouseButton();

            ViewModel.ProcessInput(new MouseClickEvent(
                Position: new Vector2((float)position.X, (float)position.Y),
                Button: button,
                InputType: InputType.Up));
        }

        private void GlRenderingControl_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var position = e.GetPosition(GlRenderingControl);
            ViewModel.ProcessInput(new MouseWheelEvent(new Vector2((float)position.X, (float)position.Y), e.Delta));
        }

        private void Window_OnKeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key.ToEngineKey();

            if (key != Key.Unknown)
            {
                ViewModel.ProcessInput(new KeyEvent(key, InputType.Down));
            }
        }

        private void LoadObject_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "3D Models (*.obj;*.stl)|*.obj;*.stl|All files (*.*)|*.*",
                Title = "Select a 3D Model"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                ViewModel.LoadMesh(openFileDialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load model:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetDeformations_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.Deformers.TwistAngle = 0f;
            ViewModel.Deformers.BendAngle = 0f;
            ViewModel.Deformers.TwistPivot = 0.5f;
            ViewModel.Deformers.BendPivot = 0.5f;
        }

        #endregion
    }
}
