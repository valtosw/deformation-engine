using Wpf = System.Windows.Input;
using Engine = Visualization.Interaction.Input;

namespace Visualization.UI.Extensions
{
    public static class KeyExtensions
    {
        public static Engine.Key ToEngineKey(this Wpf.Key key)
        {
            return key switch
            {
                Wpf.Key.F => Engine.Key.F,
                _ => throw new NotSupportedException("Unsupported key: " + key)
            };
        }
    }
}
