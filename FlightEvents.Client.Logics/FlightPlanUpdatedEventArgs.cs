using System;

namespace FlightEvents.Client.Logics
{
    public class FlightPlanUpdatedEventArgs : EventArgs
    {
        public FlightPlanUpdatedEventArgs(FlightPlanData flightPlan)
        {
            FlightPlan = flightPlan;
        }

        public FlightPlanData FlightPlan { get; }
    }
}
