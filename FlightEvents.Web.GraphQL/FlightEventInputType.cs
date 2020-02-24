using FlightEvents.Data;
using HotChocolate.Types;

namespace FlightEvents.Web.GraphQL
{
    public class FlightEventInputType : InputObjectType<FlightEvent>
    {
        protected override void Configure(IInputObjectTypeDescriptor<FlightEvent> descriptor)
        {
            descriptor.Field(o => o.Id).Ignore();
            descriptor.Field(o => o.CreatedDateTime).Ignore();
            descriptor.Field(o => o.Code).Ignore();
            descriptor.Field(o => o.Name).Type<NonNullType<StringType>>();
        }
    }
}
