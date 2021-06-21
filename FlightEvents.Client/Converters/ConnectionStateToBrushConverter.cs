using FlightEvents.Client.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FlightEvents.Client.Converters
{
    public class ConnectionStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConnectionState state)
            {
                switch (state)
                {
                    case ConnectionState.Failed: return new SolidColorBrush(Colors.Red);
                    case ConnectionState.Idle: return new SolidColorBrush(Colors.Gray);
                    case ConnectionState.Connecting: return new SolidColorBrush(Colors.LightGreen);
                    case ConnectionState.Connected: return new SolidColorBrush(Colors.Green);
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
