using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Helpers
{
    /// <summary>
    /// Converter to calculate total hours by employee and stage
    /// </summary>
    public class EmployeeHoursConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<LaborEntry> entries && parameter is string paramValue)
            {
                // Split the parameter to get employee name and stage (if present)
                string[] parts = paramValue.Split('|');
                string employeeName = parts[0].Trim();
                string stageName = parts.Length > 1 ? parts[1].Trim() : null;

                // Filter by employee
                var employeeEntries = entries.Where(e => e.Employee?.Name == employeeName);

                // Filter by stage if specified
                if (!string.IsNullOrEmpty(stageName))
                {
                    employeeEntries = employeeEntries.Where(e => e.Stage?.StageName == stageName);
                }

                // Sum the hours
                return employeeEntries.Sum(e => e.Hours);
            }

            return 0m;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
