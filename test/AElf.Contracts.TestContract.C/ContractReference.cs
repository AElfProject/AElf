using AElf.Contracts.TestContract.A;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.C
{
    public class CContractState : ContractState
    {
        public MappedState<Address, StringValue> CState { get; set; }
        internal AContractContainer.AContractReferenceState AContract { get; set; }
    }
}