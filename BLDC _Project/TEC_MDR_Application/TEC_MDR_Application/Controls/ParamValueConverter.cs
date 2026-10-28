using System;
using System.Globalization;
using System.Windows.Data;

namespace BLDC_Demo.Controls
{
    /// <summary>
    /// Used by ParamRowTemplate in CommMotorSelectControl.xaml.
    /// values[0] = SubNode instance (ancestor ItemsControl's DataContext)
    /// values[1] = PropertyName (string) to read off the SubNode via reflection
    /// values[2] = optional Suffix (string) appended to the formatted value
    /// </summary>
    public class ParamValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null
                || !(values[1] is string propertyName) || string.IsNullOrEmpty(propertyName))
            {
                return string.Empty;
            }

            object subNode = values[0];
            string suffix = values.Length > 2 ? values[2] as string : null;

            var prop = subNode.GetType().GetProperty(propertyName);
            if (prop == null)
                return "N/A";

            object rawValue = prop.GetValue(subNode);
            return suffix != null ? $"{rawValue}{suffix}" : (rawValue?.ToString() ?? string.Empty);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
