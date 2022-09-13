using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS1;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests.TestContract;

public class ContractState : Sdk.CSharp.State.ContractState
{
    public MappedState<string, MethodFees> TransactionFees { get; set; }
}