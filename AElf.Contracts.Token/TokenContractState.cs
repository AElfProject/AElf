using AElf.Common;
using AElf.Sdk.CSharp.State;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Token
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

    public class ChargedFeesMappedState : MappedState<Address, ulong>
    {
    }

    public class FeePoolAddress : SingletonState<Address>
    {
    }

    public class MethodFeesMappedState : MappedState<string, ulong>
    {
    }

    public class TokenContractState : ContractState
    {
        public MethodFeesMappedState MethodFees { get; set; }
        public BoolState Initialized { get; set; }
        public TokenInfoState TokenInfo { get; set; }
        public BalanceMappedState Balances { get; set; }
        public AllowanceMappedState Allowances { get; set; }
        public ChargedFeesMappedState ChargedFees { get; set; }
        public FeePoolAddress FeePoolAddress { get; set; }
        public SingletonState<Address> ConsensusContractAddress { get; set; }
    }
}