using System;
using System.Globalization;
using System.Windows.Data;

namespace FlightEvents.Client.Converters
{
    public class DateTimeOffsetToLocalTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTimeOffset dateTime)
            {
                return dateTime.ToLocalTime();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
