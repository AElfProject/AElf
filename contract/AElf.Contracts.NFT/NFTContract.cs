using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFT
{
    public partial class NFTContract : NFTContractContainer.NFTContractBase
    {
        public override Empty Create(CreateInput input)
        {
            MakeSureTokenContractAddressSet();
            MakeSureRandomNumberProviderContractAddressSet();
            var symbol = GetSymbol(input.NftType);
            State.TokenContract.Create.Send(new MultiToken.CreateInput
            {
                Symbol = symbol,
                Decimals = 0, // Fixed
                Issuer = Context.Sender,
                IsBurnable = true,
                IssueChainId = input.IssueChainId,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply
            });
            return new Empty();
        }
    }
}