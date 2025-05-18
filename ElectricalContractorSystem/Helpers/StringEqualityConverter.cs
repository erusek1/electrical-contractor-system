using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Converts a string value to a boolean indicating if it equals a parameter
    /// </summary>
    public class StringEqualityConverter : IValueConverter
    {
        /// <summary>
        /// Checks if value equals parameter
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString().Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Converts back - not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a string to Visibility.Visible if empty, otherwise Visibility.Collapsed
    /// </summary>
    public class StringEmptyToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts an empty string to Visible, otherwise Collapsed
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Converts back - not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a string to Visibility.Visible if not empty, otherwise Visibility.Collapsed
    /// </summary>
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a non-empty string to Visible, otherwise Collapsed
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Converts back - not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
