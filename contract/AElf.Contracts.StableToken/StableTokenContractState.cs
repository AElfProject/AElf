using System;
using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS10;

namespace AElf.Contracts.StableToken
{
    public partial class StableTokenContractState : ContractState
    {
        public SingletonState<SymbolList> StableTokenSymbolList { get; set; }
    }
}