using Acs1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Events
{
    public partial class EventsContract
    {
        public override MethodFees GetMethodFee(StringValue input)
        {
            return new MethodFees();
        }

        public override Empty SetMethodFee(MethodFees input)
        {
            return new Empty();
        }
    }
}