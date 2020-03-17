using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FlightEvents.Client.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public bool Reversed { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null) ^ Reversed ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
