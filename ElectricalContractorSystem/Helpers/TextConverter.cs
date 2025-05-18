using System;
using System.Globalization;
using System.Windows.Data;

namespace ElectricalContractorSystem.Helpers
{
    public class TextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return value?.ToString() ?? string.Empty;

            string[] options = parameter.ToString().Split('|');
            
            // Boolean to text conversion
            if (value is bool boolValue && options.Length >= 2)
            {
                return boolValue ? options[0] : options[1];
            }
            
            // Numeric to text conversion
            if (value is int intValue && options.Length > intValue)
            {
                return options[intValue];
            }
            
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
