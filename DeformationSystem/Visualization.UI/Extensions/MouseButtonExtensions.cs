using Wpf = System.Windows.Input;
using Engine = Visualization.Interaction.Input;

namespace Visualization.UI.Extensions
{
    public static class MouseButtonExtensions
    {
        public static Engine.MouseButton ToEngineMouseButton(this Wpf.MouseButton mouseButton)
        {
            return mouseButton switch
            {
                Wpf.MouseButton.Left   => Engine.MouseButton.Left,
                Wpf.MouseButton.Right  => Engine.MouseButton.Right,
                Wpf.MouseButton.Middle => Engine.MouseButton.Middle,
                _                      => Engine.MouseButton.Unknown
            };
        }
    }
}
