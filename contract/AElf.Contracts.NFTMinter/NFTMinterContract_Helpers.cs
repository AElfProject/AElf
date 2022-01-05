using AElf.Contracts.NFT;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMinter
{
    public partial class NFTMinterContract
    {
        private Address GetNFTProtocolCreator(string symbol)
        {
            return State.NFTContract.GetNFTProtocolInfo.Call(new StringValue {Value = symbol}).Creator;
        }

        private void CheckSymbolAndPermission(string symbol)
        {
            Assert(!string.IsNullOrEmpty(symbol.Trim()), "Symbol is empty.");
            Assert(GetNFTProtocolCreator(symbol) == Context.Sender, "No permission.");
        }

        private NFTProtocolInfo ValidNFTProtocol(string symbol)
        {
            var nftProtocol = State.NFTContract.GetNFTProtocolInfo.Call(new StringValue {Value = symbol});
            Assert(nftProtocol.NftType == NFTType.Badges.ToString(), "Invalid NFT Protocol.");
            var minterList = State.NFTContract.GetMinterList.Call(new StringValue {Value = symbol});
            Assert(minterList.Value.Contains(Context.Self), $"NFT Minter Contract is not in minter list of {symbol}");
            return nftProtocol;
        }
    }
}