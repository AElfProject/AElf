using AElf.Standards.ACS0;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Sdk.CSharp.Tests.TestContract
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

    public class MethodFeesMappedState : MappedState<string, ulong>
    {
    }

    public class TokenContractState : ContractState
    {
        public MethodFeesMappedState TransactionFees { get; set; }
        public BoolState Initialized { get; set; }
        public TokenInfoState TokenInfo { get; set; }
        public BalanceMappedState Balances { get; set; }
        public AllowanceMappedState Allowances { get; set; }

        public ReadonlyState<string> NativeTokenSymbol { get; set; }

        internal ACS0Container.ACS0ReferenceState ZeroContract { get; set; }
    }
}