using System;
using System.Globalization;
using System.Windows.Data;

namespace FlightEvents.Client.Converters
{
    public class ValueToBooleanConverter : IValueConverter
    {
        public bool Reversed { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value?.ToString() == parameter?.ToString()) ^ Reversed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
