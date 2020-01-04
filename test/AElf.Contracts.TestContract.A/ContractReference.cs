using AElf.Contracts.TestContract.B;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.A
{
    public class AContractState : ContractState
    {
        public MappedState<Address, StringValue> AState { get; set; }
        internal BContractContainer.BContractReferenceState BContract { get; set; }
    }
}