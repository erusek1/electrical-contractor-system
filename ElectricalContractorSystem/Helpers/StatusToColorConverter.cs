using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Converts job status values to corresponding colors
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a job status (Estimate, In Progress, Complete) to a color
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return new SolidColorBrush(Colors.Gray);

            string status = value.ToString();

            switch (status.ToLower())
            {
                case "estimate":
                    return new SolidColorBrush((Color)Application.Current.Resources["AccentColor"]);
                case "in progress":
                    return new SolidColorBrush(Colors.CornflowerBlue);
                case "complete":
                    return new SolidColorBrush(Colors.ForestGreen);
                default:
                    return new SolidColorBrush(Colors.Gray);
            }
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
    /// Converts job status values to corresponding brush resources
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts a job status (Estimate, In Progress, Complete) to a brush resource
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Application.Current.Resources["TextPrimaryBrush"];

            string status = value.ToString();

            switch (status.ToLower())
            {
                case "estimate":
                    return Application.Current.Resources["EstimateBrush"];
                case "in progress":
                    return Application.Current.Resources["InProgressBrush"];
                case "complete":
                    return Application.Current.Resources["CompleteBrush"];
                default:
                    return Application.Current.Resources["TextPrimaryBrush"];
            }
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
