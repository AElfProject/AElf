using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFT
{
    public partial class NFTContract : NFTContractContainer.NFTContractBase
    {
        public override Empty CreateNFT(CreateNFTInput input)
        {
            MakeSureTokenContractAddressSet();
            MakeSureRandomNumberProviderContractAddressSet();
            var symbol = GetSymbol(input.NftType);
            State.TokenContract.Create.Send(new MultiToken.CreateInput
            {
                Symbol = symbol,
                Decimals = 0, // Fixed
                Issuer = Context.Sender,
                IsBurnable = input.IsBurnable,
                IssueChainId = input.IssueChainId,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                ExternalInfo = new MultiToken.ExternalInfo
                {
                    Value = {input.ExternalInfo.Value}
                }
            });
            Context.Fire(new NFTCreated
            {
                Symbol = symbol,
                Issuer = Context.Sender,
                IsBurnable = input.IsBurnable,
                IssueChainId = input.IssueChainId,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                ExternalInfo = input.ExternalInfo
            });
            return new Empty();
        }
    }
}