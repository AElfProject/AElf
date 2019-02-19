using AElf.Common;
using AElf.Sdk.CSharp.State;

namespace AElf.Benchmark.TestContract
{
    public class TestTokenContractState : ContractState
    {
        public MappedState<Address, ulong> Balances { get; set; }
    }
}