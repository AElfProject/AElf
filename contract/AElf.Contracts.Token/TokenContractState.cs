using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Token;

public partial class TokenContractState : ContractState
{
    public MappedState<string, TokenInfo> TokenInfos { get; set; }
    public MappedState<Address, string, long> Balances { get; set; }
    public MappedState<Address, Address, string, long> Allowances { get; set; }
}