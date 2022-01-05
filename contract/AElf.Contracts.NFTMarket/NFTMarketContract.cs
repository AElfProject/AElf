using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMarket
{
    public partial class NFTMarketContract : NFTMarketContractContainer.NFTMarketContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            State.NFTContract.Value = input.NftContractAddress;
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            return new Empty();
        }
    }
}