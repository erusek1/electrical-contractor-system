using System;
using System.Globalization;
using System.Windows.Data;

namespace ElectricalContractorSystem.Helpers
{
    public class StringEqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return System.Windows.Visibility.Collapsed;
            
            if (value is string stringValue && parameter is string parameterValue)
            {
                return stringValue.Equals(parameterValue, StringComparison.OrdinalIgnoreCase) 
                    ? System.Windows.Visibility.Visible 
                    : System.Windows.Visibility.Collapsed;
            }
            
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
