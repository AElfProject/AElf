using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests.TestContract;
using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS1;

namespace AElf.Contracts.TestContract.TransactionFees;

public class TransactionFeesContractState : ContractState
{
    public MappedState<int, int> TestInfo { get; set; }
    public MappedState<string, MethodFees> TransactionFees { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

    internal ContractContainer.ContractReferenceState Acs8Contract { get; set; }
}