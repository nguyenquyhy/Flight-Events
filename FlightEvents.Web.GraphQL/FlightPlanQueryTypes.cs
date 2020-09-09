using FlightEvents.Data;
using HotChocolate;
using HotChocolate.Types;
using System.Threading.Tasks;

namespace FlightEvents.Web.GraphQL
{
    public class FlightPlanQueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Field<FlightPlanResolver>(o => o.GetId(default)).Type<NonNullType<StringType>>();
            descriptor.Field<FlightPlanResolver>(o => o.GetDownloadUrl(default, default)).Type<NonNullType<StringType>>();
            descriptor.Field<FlightPlanResolver>(o => o.GetFlightPlanData(default, default)).Name("data").Type<NonNullType<FlightPlanDataType>>();
        }
    }

    public class FlightPlanResolver
    {
        public string GetId([Parent]string id) => id;
        public Task<string> GetDownloadUrl([Parent]string id, [Service]IFlightPlanStorage flightPlanStorage) => flightPlanStorage.GetFlightPlanUrlAsync(id);
        public Task<FlightPlanData> GetFlightPlanData([Parent]string id, [Service]IFlightPlanStorage flightPlanStorage) => flightPlanStorage.GetFlightPlanAsync(id);
    }

    public class FlightPlanDataType : ObjectType<FlightPlanData>
    {

    }
}
