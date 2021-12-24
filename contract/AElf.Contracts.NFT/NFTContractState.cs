using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.NFT
{
    public partial class NFTContractState : ContractState
    {
        public Int64State NftProtocolNumberFlag { get; set; }
        public Int32State CurrentSymbolNumberLength { get; set; }
        public MappedState<long, bool> IsCreatedMap { get; set; }

        /// <summary>
        /// Symbol -> Addresses have permission to mint this token
        /// </summary>
        public MappedState<string, MinterList> MinterListMap { get; set; }
        public MappedState<Hash, NFTInfo> NftInfoMap { get; set; }

        /// <summary>
        /// Token Hash -> Owner Address -> Balance
        /// </summary>
        public MappedState<Hash, Address, long> BalanceMap { get; set; }

        public MappedState<string, NFTProtocolInfo> NftProtocolMap { get; set; }

        /// <summary>
        /// Token Hash -> Owner Address -> Spender Address -> Approved Amount
        /// Need to record approved by whom.
        /// </summary>
        public MappedState<Hash, Address, Address, long> AllowanceMap { get; set; }

        public MappedState<Hash, AssembledNfts> AssembledNftsMap { get; set; }
        public MappedState<Hash, AssembledFts> AssembledFtsMap { get; set; }

        public MappedState<string, string> NFTTypeShortNameMap { get; set; }
        public MappedState<string, string> NFTTypeFullNameMap { get; set; }

        public SingletonState<Address> ParliamentDefaultAddress { get; set; }

        public SingletonState<NFTTypes> NFTTypes { get; set; }
    }
}