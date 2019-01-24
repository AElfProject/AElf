using AElf.Common;
using AElf.Sdk.CSharp.State;
using AElf.Sdk.CSharp;

namespace AElf.Sdk.CSharp2.Tests.TestContract
{
    public class TokenInfoState : StructuredState
    {
        public StringState Symbol { get; set; }
        public StringState TokenName { get; set; }
        public UInt64State TotalSupply { get; set; }
        public UInt32State Decimals { get; set; }
    }

    public class BalanceMappedState : MappedState<Address, ulong>
    {
    }

    public class AllowanceMappedState : MappedState<Address, Address, ulong>
    {
    }

    public class TokenContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public TokenInfoState TokenInfo { get; set; }
        public BalanceMappedState Balances { get; set; }
        public AllowanceMappedState Allowances { get; set; }
    }
}