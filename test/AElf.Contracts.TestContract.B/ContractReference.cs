using AElf.Contracts.TestContract.C;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.B
{
    public class BContractState : ContractState
    {
        public MappedState<Address, StringValue> BState { get; set; }
        internal CContractContainer.CContractReferenceState CContract { get; set; }
    }
}