using System;
using System.Collections.Generic;

namespace FlightEvents.Client.Logics
{
    public class FlightPlanUpdatedEventArgs : EventArgs
    {
        public FlightPlanUpdatedEventArgs(FlightPlanData flightPlan, List<string> atcConnectionIds)
        {
            AtcConnectionIds = atcConnectionIds;
            FlightPlan = flightPlan;
        }

        public List<string> AtcConnectionIds { get; }
        public FlightPlanData FlightPlan { get; }
    }
}
