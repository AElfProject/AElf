using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContractState : ContractState
    {
        public SingletonState<Address> Admin { get; set; }

        public SingletonState<Address> ServiceFeeReceiver { get; set; }

        public Int32State ServiceFeeRate { get; set; }

        /// <summary>
        /// Symbol -> Token Id -> Owner -> List NFT Info
        /// </summary>
        public MappedState<string, long, Address, ListedNFTInfo> ListedNftInfoMap { get; set; }

        public MappedState<string, long, OfferList> OfferListMap { get; set; }
    }
}