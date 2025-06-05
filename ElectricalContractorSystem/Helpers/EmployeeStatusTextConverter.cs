using System;
using System.Globalization;
using System.Windows.Data;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Converter for employee status text
    /// </summary>
    public class EmployeeStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // For now, return status text based on employee name
            // In full implementation, this would check actual hours vs target
            string employeeName = value?.ToString() ?? "";
            
            // Simulate status based on name for demo
            switch (employeeName.ToLower())
            {
                case "erik":
                case "lee":
                    return "Complete";
                case "carlos":
                case "jake":
                    return "Partial";
                default:
                    return "Missing";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}