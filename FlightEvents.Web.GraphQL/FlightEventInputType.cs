using FlightEvents.Data;
using HotChocolate.Types;
using System;

namespace FlightEvents.Web.GraphQL
{
    public class FlightEventAddInputType : InputObjectType<FlightEvent>
    {
        protected override void Configure(IInputObjectTypeDescriptor<FlightEvent> descriptor)
        {
            descriptor.Name("FlightEventAdd");

            descriptor.Field(o => o.Id).Ignore();
            descriptor.Field(o => o.CreatedDateTime).Ignore();
            descriptor.Field(o => o.UpdatedDateTime).Ignore();
            descriptor.Field(o => o.Code).Ignore();
            descriptor.Field(o => o.Name).Type<NonNullType<StringType>>();
        }
    }

    public class FlightEventUpdateInputType : InputObjectType<FlightEvent>
    {
        protected override void Configure(IInputObjectTypeDescriptor<FlightEvent> descriptor)
        {
            descriptor.Name("FlightEventUpdate");

            descriptor.Field(o => o.CreatedDateTime).Ignore();
            descriptor.Field(o => o.UpdatedDateTime).Ignore();
            descriptor.Field(o => o.Code).Ignore();
            descriptor.Field(o => o.StartDateTime).Type<DateTimeType>().DefaultValue(default(DateTimeOffset));
        }
    }
}
