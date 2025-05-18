using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Converts a boolean value to Visibility
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts true to Visible, false to Collapsed
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;
            
            if (value is bool)
            {
                boolValue = (bool)value;
            }
            else if (value is bool?)
            {
                bool? nullable = (bool?)value;
                boolValue = nullable.HasValue ? nullable.Value : false;
            }
            
            // Check if we need to invert the value
            if (parameter != null && parameter.ToString().ToLower() == "invert")
            {
                boolValue = !boolValue;
            }
            
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Converts back - Visible to true, Collapsed/Hidden to false
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Visibility))
                return false;
                
            bool result = (Visibility)value == Visibility.Visible;
            
            // Check if we need to invert the value
            if (parameter != null && parameter.ToString().ToLower() == "invert")
            {
                result = !result;
            }
            
            return result;
        }
    }
}
