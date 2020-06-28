using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FlightEvents.Client.Converters
{
    public class StatesToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is ConnectionState simconnectState && values[1] is bool trackingState)
            {
                switch (simconnectState)
                {
                    case ConnectionState.Failed: return new SolidColorBrush(Colors.Red);
                    case ConnectionState.Idle: return new SolidColorBrush(Colors.Gray);
                    case ConnectionState.Connecting: return new SolidColorBrush(Colors.Gray);
                    case ConnectionState.Connected:
                        if (trackingState) return new SolidColorBrush(Colors.Green);
                        else return new SolidColorBrush(Colors.Gray);
                }
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
