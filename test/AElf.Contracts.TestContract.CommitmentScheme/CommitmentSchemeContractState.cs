using Acs1;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.CommitmentScheme
{
    public partial class CommitmentSchemeContractState : ContractState
    {
        public MappedState<string, MethodFees> TransactionFees { get; set; }

        public MappedState<Address, Hash> Commitments { get; set; }
        public MappedState<Address, PreviousInValueInformation> PreviousInValueInformations { get; set; }
    }
}