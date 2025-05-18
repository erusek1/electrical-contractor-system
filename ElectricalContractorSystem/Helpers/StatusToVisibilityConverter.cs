using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Converts status values to Visibility based on matching a parameter
    /// </summary>
    public class StatusToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a status value to Visibility.Visible if it matches the parameter, otherwise Visibility.Collapsed
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string status = value.ToString().ToLower();
            string paramStatus = parameter.ToString().ToLower();

            // If the status matches the parameter, return Visible
            if (status == paramStatus)
                return Visibility.Visible;

            // Special case: if parameter is "active" and status is not "complete", return Visible
            if (paramStatus == "active" && status != "complete")
                return Visibility.Visible;

            return Visibility.Collapsed;
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
    /// Converts status values to inverse Visibility based on matching a parameter
    /// </summary>
    public class StatusToInverseVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a status value to Visibility.Collapsed if it matches the parameter, otherwise Visibility.Visible
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Visible;

            string status = value.ToString().ToLower();
            string paramStatus = parameter.ToString().ToLower();

            // If the status matches the parameter, return Collapsed
            if (status == paramStatus)
                return Visibility.Collapsed;

            // Special case: if parameter is "active" and status is not "complete", return Collapsed
            if (paramStatus == "active" && status != "complete")
                return Visibility.Collapsed;

            return Visibility.Visible;
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
