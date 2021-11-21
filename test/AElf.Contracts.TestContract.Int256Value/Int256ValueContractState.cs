using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.Int256Value
{
    public class Int256ValueContractState : ContractState
    {
        public SingletonState<UInt256Value> UInt256State { get; set; }
        public SingletonState<Types.Int256Value> Int256State { get; set; }
    }
}