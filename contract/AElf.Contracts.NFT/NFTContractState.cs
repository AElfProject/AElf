using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS1;

namespace AElf.Contracts.NFT
{
    public partial class NFTContractState : ContractState
    {
        public Int64State NftCount { get; set; }
        public Int32State SymbolNumberLength { get; set; }
        public MappedState<long, bool> IsCreatedMap { get; set; }
    }
}