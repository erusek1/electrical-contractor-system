using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Converts string equality to visibility
    /// </summary>
    public class StringEqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            return value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
