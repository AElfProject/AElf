using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.TestContract.BigIntValue;

public class BigIntValueContractState : ContractState
{
    public SingletonState<Types.BigIntValue> BigIntState { get; set; }
}