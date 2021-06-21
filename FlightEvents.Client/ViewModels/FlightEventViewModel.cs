using FlightEvents.Data;
using Humanizer;
using System;
using System.Timers;
using System.Windows.Media;

namespace FlightEvents.Client.ViewModels
{
    public class FlightEventViewModel : BaseViewModel
    {
        private readonly Timer timer;

        public FlightEvent Model { get; }

        private string friendlyDateTime;
        public string FriendlyDateTime { get => friendlyDateTime; set => SetProperty(ref friendlyDateTime, value); }

        private Color backgroundColor;
        public Color BackgroundColor { get => backgroundColor; set => SetProperty(ref backgroundColor, value); }

        public FlightEventViewModel(FlightEvent flightEvent)
        {
            Model = flightEvent;
            //SetFriendlyDateTime();
            //SetBackgroundColor();

            Timer_Elapsed(null, null);

            timer = new Timer(60000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SetFriendlyDateTime();
            SetBackgroundColor();
        }

        private void SetFriendlyDateTime()
        {
            if (Model.StartDateTime <= DateTimeOffset.UtcNow)
            {
                if (Model.EndDateTime.HasValue)
                {
                    if (Model.EndDateTime.Value <= DateTime.UtcNow)
                    {
                        FriendlyDateTime = "Ended";
                    }
                    else
                    {
                        FriendlyDateTime = "Started " + Model.StartDateTime.Humanize();
                    }
                }
                else
                {
                    if (Model.StartDateTime + TimeSpan.FromHours(4) <= DateTime.UtcNow)
                    {
                        FriendlyDateTime = "Ended";
                    }
                    else
                    {
                        FriendlyDateTime = "Started " + Model.StartDateTime.Humanize();
                    }
                }
            }
            else
            {
                FriendlyDateTime = Model.StartDateTime.Humanize();
            }
        }

        private void SetBackgroundColor()
        {
            BackgroundColor = Model.StartDateTime < DateTimeOffset.Now ? Colors.LightCyan : Colors.Transparent;
        }
    }
}
