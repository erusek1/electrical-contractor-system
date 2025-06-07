using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ElectricalContractorSystem.Models;

namespace ElectricalContractorSystem.Helpers
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is EstimateStatus status)
            {
                switch (status)
                {
                    case EstimateStatus.Draft:
                        return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // Gray
                    case EstimateStatus.Sent:
                        return new SolidColorBrush(Color.FromRgb(52, 152, 219)); // Blue
                    case EstimateStatus.Approved:
                        return new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Green
                    case EstimateStatus.Rejected:
                        return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red
                    case EstimateStatus.Expired:
                        return new SolidColorBrush(Color.FromRgb(243, 156, 18)); // Orange
                    case EstimateStatus.Converted:
                        return new SolidColorBrush(Color.FromRgb(155, 89, 182)); // Purple
                    default:
                        return new SolidColorBrush(Colors.Gray);
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