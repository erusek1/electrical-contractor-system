using System;
using System.Globalization;
using System.Windows.Data;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Converts employee hours to appropriate status text
    /// </summary>
    public class HoursStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal hours)
            {
                // Complete - 40 hours exactly
                if (hours == 40)
                {
                    return "Complete";
                }
                // Partial - some hours entered
                else if (hours > 0)
                {
                    return "Partial";
                }
                // Missing - no hours entered
                else
                {
                    return "Missing";
                }
            }

            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
