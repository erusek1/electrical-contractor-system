using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Converts employee hours to appropriate status color
    /// </summary>
    public class HoursStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal hours)
            {
                // Complete - 40 hours exactly
                if (hours == 40)
                {
                    return new SolidColorBrush(Colors.Green);
                }
                // Partial - some hours entered
                else if (hours > 0)
                {
                    return new SolidColorBrush(Colors.Orange);
                }
                // Missing - no hours entered
                else
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
