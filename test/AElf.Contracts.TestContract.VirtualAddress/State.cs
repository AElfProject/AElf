using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS1;

namespace AElf.Contracts.TestContract.VirtualAddress;

public partial class State : ContractState
{
    public MappedState<string, MethodFees> TransactionFees { get; set; }
}