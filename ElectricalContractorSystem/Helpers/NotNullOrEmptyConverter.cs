using System;
using System.Globalization;
using System.Windows.Data;

namespace ElectricalContractorSystem.Helpers
{
    public class NotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return false;
            
            if (value is string stringValue)
            {
                return !string.IsNullOrEmpty(stringValue);
            }
            
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
