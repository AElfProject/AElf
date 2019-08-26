using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Events
{
    public class EventsContract : EventsContractContainer.EventsContractBase
    {
        //action
        public override Empty SwitchToken(SwitchTokenInput input)
        {
            return new Empty();
        }

        //view
        public override SwitchTokensOutput QuerySwitchRecords(Address input)
        {
            var records = State.SwitchRecords[input];
            if(records == null)
                return new SwitchTokensOutput();

            return records;
        }
    }
}