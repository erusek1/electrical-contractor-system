using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Converter for employee status background color
    /// </summary>
    public class EmployeeStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // For now, return different colors based on employee name
            // In full implementation, this would check actual hours vs target
            string employeeName = value?.ToString() ?? "";
            
            // Simulate status based on name for demo
            switch (employeeName.ToLower())
            {
                case "erik":
                case "lee":
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green - Complete
                case "carlos":
                case "jake":
                    return new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow - Partial
                default:
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red - Missing
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}