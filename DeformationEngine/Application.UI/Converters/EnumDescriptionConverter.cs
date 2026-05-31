using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Application.UI.Converters
{
    public sealed class EnumDescriptionConverter : IValueConverter
    {
        #region Public Logic

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Enum enumValue)
            {
                return value?.ToString() ?? string.Empty;
            }

            var description = GetEnumDescription(enumValue);

            return description;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Private Logic

        private static string GetEnumDescription(Enum enumValue)
        {
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

            if (fieldInfo is not null)
            {
                var attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attributes.Length > 0 && attributes[0] is DescriptionAttribute descriptionAttribute)
                {
                    return descriptionAttribute.Description;
                }
            }

            return enumValue.ToString();
        }

        #endregion
    }
}