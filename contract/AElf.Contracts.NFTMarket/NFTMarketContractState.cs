using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContractState : ContractState
    {
        public SingletonState<Address> Admin { get; set; }

        public SingletonState<Address> ServiceFeeReceiver { get; set; }

        public Int32State ServiceFeeRate { get; set; }
        public Int64State ServiceFee { get; set; }

        public SingletonState<StringList> GlobalTokenWhiteList { get; set; }

        /// <summary>
        /// Symbol -> Token Id -> Owner -> List NFT Info
        /// </summary>
        public MappedState<string, long, Address, ListedNFTInfo> ListedNftInfoMap { get; set; }

        public MappedState<string, long, WhiteListAddressPriceList> WhiteListAddressPriceListMap { get; set; }

        /// <summary>
        /// Symbol -> Token Id -> Offer Maker -> Offer List
        /// </summary>
        public MappedState<string, long, Address, OfferList> OfferListMap { get; set; }
        public MappedState<string, long, Address, OfferList> BidListMap { get; set; }

        /// <summary>
        /// Symbol -> Token Id -> Offer Address List
        /// </summary>
        public MappedState<string, long, AddressList> OfferAddressListMap { get; set; }
        public MappedState<string, long, AddressList> BidAddressListMap { get; set; }

        /// <summary>
        /// Symbol -> Token Id -> Royalty
        /// </summary>
        public MappedState<string, int> RoyaltyMap { get; set; }

        public MappedState<string, Address> RoyaltyFeeReceiverMap { get; set; }
        public MappedState<string, long, int> CertainNFTRoyaltyMap { get; set; }
        public MappedState<string, StringList> TokenWhiteListMap { get; set; }

        public MappedState<string, CustomizeInfo> CustomizeInfoMap { get; set; }
        public MappedState<string, long, RequestInfo> RequestInfoMap { get; set; }

        public MappedState<string, long, EnglishAuctionInfo> EnglishAuctionInfoMap { get; set; }
        public MappedState<string, long, DutchAuctionInfo> DutchAuctionInfoMap { get; set; }
    }
}