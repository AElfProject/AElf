using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.Events
{
    public class EventsContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public MappedState<Address, SwitchTokensOutput> SwitchRecords { get; set; }

        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}