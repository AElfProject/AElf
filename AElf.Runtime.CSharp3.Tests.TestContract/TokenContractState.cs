using AElf.Common;
using AElf.Sdk.CSharp.State;

namespace AElf.Runtime.CSharp3.Tests.TestContract
{
    public class TokenContractState : ContractState
    {
        public StringState NativeTokenSymbol { get; set; }
        public MappedState<string, long> MethodFees { get; set; }
        public MappedState<string, TokenInfo> TokenInfos { get; set; }
        public MappedState<Address, string, long> Balances { get; set; }
        public MappedState<Address, Address, string, long> Allowances { get; set; }
        public MappedState<Address, string, long> ChargedFees { get; set; }
        public SingletonState<Address> FeePoolAddress { get; set; }
    }
}