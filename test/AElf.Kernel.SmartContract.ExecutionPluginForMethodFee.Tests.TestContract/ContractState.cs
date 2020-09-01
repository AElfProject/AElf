using AElf.Standards.ACS1;
using AElf.Sdk.CSharp.State;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests.TestContract
{
    public class ContractState : AElf.Sdk.CSharp.State.ContractState
    {
        public MappedState<string,MethodFees> TransactionFees { get; set; }
    }
}