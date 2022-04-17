using FlightEvents.Data;
using HotChocolate.Types;

namespace FlightEvents.Web.GraphQL
{
    public class FlightEventChecklistItemTypeEnumType : EnumType<FlightEventChecklistItemType>
    {
        protected override void Configure(IEnumTypeDescriptor<FlightEventChecklistItemType> descriptor)
        {
            descriptor.Value(FlightEventChecklistItemType.MicrosoftSimulator).Name("MICROSOFTSIMULATOR");
        }
    }
}
