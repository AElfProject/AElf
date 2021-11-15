using AElf.Sdk.CSharp;
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
                IsBurnable = input.IsBurnable,
                IssueChainId = input.IssueChainId,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                ExternalInfo = new MultiToken.ExternalInfo
                {
                    Value = {input.MetaData.Value}
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
                MinerList = input.MinerList,
                MetaData = input.MetaData
            });
            return new Empty();
        }
    }
}