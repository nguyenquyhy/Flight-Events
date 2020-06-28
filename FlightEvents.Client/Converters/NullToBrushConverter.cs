using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FlightEvents.Client.Converters
{
    public class NullToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case null: return new SolidColorBrush(Colors.Gray); 
                default: return new SolidColorBrush(Colors.Green);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
