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

namespace Application.UI.Views
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

        protected override void OnClosed(EventArgs eventArguments)
        {
            base.OnClosed(eventArguments);
            GlRenderingControl.Dispose();
            ViewModel.Dispose();
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

        private void GlRenderingControl_OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            ViewModel.Resize((int)sizeChangedEventArgs.NewSize.Width, (int)sizeChangedEventArgs.NewSize.Height);
        }

        private void GlRenderingControl_OnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var position = mouseEventArgs.GetPosition(GlRenderingControl);
            ViewModel.ProcessInput(new MouseMoveEvent(new Vector2((float)position.X, (float)position.Y)));
        }

        private void GlRenderingControl_OnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            GlRenderingControl.CaptureMouse();

            var position = mouseButtonEventArgs.GetPosition(GlRenderingControl);
            var button = mouseButtonEventArgs.ChangedButton.ToEngineMouseButton();

            ViewModel.ProcessInput(new MouseClickEvent(
                Position: new Vector2((float)position.X, (float)position.Y),
                Button: button,
                InputType: InputType.Down));
        }

        private void GlRenderingControl_OnMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (GlRenderingControl.IsMouseCaptured)
            {
                GlRenderingControl.ReleaseMouseCapture();
            }

            var position = mouseButtonEventArgs.GetPosition(GlRenderingControl);
            var button = mouseButtonEventArgs.ChangedButton.ToEngineMouseButton();

            ViewModel.ProcessInput(new MouseClickEvent(
                Position: new Vector2((float)position.X, (float)position.Y),
                Button: button,
                InputType: InputType.Up));
        }

        private void GlRenderingControl_OnMouseWheel(object sender, MouseWheelEventArgs mouseWheelEventArgs)
        {
            var position = mouseWheelEventArgs.GetPosition(GlRenderingControl);
            ViewModel.ProcessInput(new MouseWheelEvent(new Vector2((float)position.X, (float)position.Y), mouseWheelEventArgs.Delta));
        }

        private void Window_OnKeyDown(object sender, KeyEventArgs keyEventArgs)
        {
            var key = keyEventArgs.Key.ToEngineKey();

            if (key != Key.Unknown)
            {
                ViewModel.ProcessInput(new KeyEvent(key, InputType.Down));
            }
        }

        private void LoadObject_OnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "3D Models (*.obj;*.stl;*.gltf;*.glb;*.dae)|*.obj;*.stl;*.gltf;*.glb;*.dae|All files (*.*)|*.*",
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
            catch (Exception exception)
            {
                MessageBoxWindow.Show(this, $"Failed to load model:\n{exception.Message}", "Error", MessageBoxButton.OK);
            }
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_OnClick(object sender, RoutedEventArgs eventArguments)
        {
            Close();
        }

        #endregion
    }
}