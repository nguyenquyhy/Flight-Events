using FlightEvents.Data;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FlightEvents.Client.Converters
{
    public class FlightEventToBackgroundBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FlightEvent flightEvent)
            {
                if (flightEvent.StartDateTime < DateTimeOffset.Now)
                {
                    return new SolidColorBrush(Colors.LightCyan);
                }
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
