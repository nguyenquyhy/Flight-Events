using FlightEvents.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace FlightEvents.Client.Converters
{
    public class ViewModelToWindowWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] is ChecklistViewModel checklistEvent && checklistEvent != null)
            {
                return 950d;
            }
            else if (values[0] is IList<FlightEvent> events && events != null && events.Count > 0)
            {
                return 640d;
            }
            return 340d;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
