using System.Windows;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using System.Windows.Input;
using Visualization.Interaction.Input;
using Visualization.UI.Extensions;
using Visualization.UI.ViewModels;
using Visualization.Rendering;
using InputType = Visualization.Interaction.Input.InputType;
using Key = Visualization.Interaction.Input.Key;

namespace Visualization.UI.Windows
{
    public sealed partial class MainWindow
    {
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

        public MainViewModel ViewModel { get; }

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
                GlRenderingControl.ReleaseMouseCapture();

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
                ViewModel.ProcessInput(new KeyEvent(key, InputType.Down));
        }
    }
}