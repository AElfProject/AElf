using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.MockParliament;

public partial class MockParliamentContractState : ContractState
{
    public SingletonState<Address> DefaultOrganizationAddress { get; set; }
}