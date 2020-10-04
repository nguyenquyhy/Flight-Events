using FlightEvents.Data;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FlightEvents.Client.Converters
{
    public class FlightEventDateTimeToVisibilityConverter : IValueConverter
    {
        public bool Reversed { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FlightEvent flightEvent)
            {
                return (flightEvent.StartDateTime < DateTimeOffset.Now) ^ Reversed ? Visibility.Visible : Visibility.Collapsed;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
