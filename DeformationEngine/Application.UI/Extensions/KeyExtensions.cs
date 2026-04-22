using Wpf = System.Windows.Input;
using Engine = Deformation.Interaction.Input;

namespace Application.UI.Extensions
{
    public static class KeyExtensions
    {
        public static Engine.Key ToEngineKey(this Wpf.Key key)
        {
            return key switch
            {
                Wpf.Key.F  => Engine.Key.F,
                Wpf.Key.P  => Engine.Key.P,
                Wpf.Key.V  => Engine.Key.V,
                Wpf.Key.D1 => Engine.Key.D1,
                Wpf.Key.D2 => Engine.Key.D2,
                Wpf.Key.D3 => Engine.Key.D3,
                Wpf.Key.D4 => Engine.Key.D4,
                Wpf.Key.D5 => Engine.Key.D5,
                Wpf.Key.D6 => Engine.Key.D6,
                Wpf.Key.D7 => Engine.Key.D7,
                _          => Engine.Key.Unknown
            };
        }
    }
}
