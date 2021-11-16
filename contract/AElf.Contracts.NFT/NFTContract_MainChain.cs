using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFT
{
    public partial class NFTContract : NFTContractContainer.NFTContractBase
    {
        /// <summary>
        /// The Create method can only be executed in aelf MainChain.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Create(CreateInput input)
        {
            MakeSureTokenContractAddressSet();
            MakeSureRandomNumberProviderContractAddressSet();
            var symbol = GetSymbol(input.NftType);
            var tokenExternalInfo = GetTokenExternalInfo(input);
            var tokenCreateInput = new MultiToken.CreateInput
            {
                Symbol = symbol,
                Decimals = 0, // Fixed
                Issuer = Context.Sender,
                IsBurnable = input.IsBurnable,
                IssueChainId = input.IssueChainId,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                ExternalInfo = tokenExternalInfo
            };
            State.TokenContract.Create.Send(tokenCreateInput);

            var baseInfo = new NFTBaseInfo
            {
                Symbol = symbol,
                BaseUri = input.BaseUri,
                CreatedCount = tokenCreateInput.TotalSupply,
                Creator = Context.Sender,
                Metadata = new Metadata {Value = {tokenExternalInfo.Value}}
            };
            State.NftBaseInfoMap[symbol] = baseInfo;

            Context.Fire(new NFTCreated
            {
                Symbol = tokenCreateInput.Symbol,
                Issuer = tokenCreateInput.Issuer,
                IsBurnable = tokenCreateInput.IsBurnable,
                IssueChainId = tokenCreateInput.IssueChainId,
                TokenName = tokenCreateInput.TokenName,
                TotalSupply = tokenCreateInput.TotalSupply,
                MetaData = baseInfo.Metadata,
                BaseUri = baseInfo.BaseUri
            });

            return new Empty();
        }

        private ExternalInfo GetTokenExternalInfo(CreateInput input)
        {
            var tokenExternalInfo = new ExternalInfo
            {
                Value = {input.MetaData.Value}
            };

            // Add NFT Type to external info.
            tokenExternalInfo.Value.Add(NftTypeMetadataKey, input.NftType.ToString());

            // Add Uri to external info.
            tokenExternalInfo.Value.Add(NftBaseUriMetadataKey, input.BaseUri);
            return tokenExternalInfo;
        }
    }
}