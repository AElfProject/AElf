using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.NFT
{
    public partial class NFTContractState : ContractState
    {
        public Int64State NftCount { get; set; }
        public Int32State SymbolNumberLength { get; set; }
        public MappedState<long, bool> IsUsedMap { get; set; }
    }
}