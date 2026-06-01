using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Application.UI.Converters
{
    public sealed class BooleanAccentBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isActive = value is true;
            return System.Windows.Application.Current.TryFindResource(isActive ? "AccentBrush" : "TextBrush") as Brush
                   ?? Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
