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
        public override StringValue Create(CreateInput input)
        {
            Assert(Context.ChainId == ChainHelper.ConvertBase58ToChainId("AELF"),
                "NFT Protocol can only be created at aelf mainchain.");
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
                TokenName = input.ProtocolName,
                TotalSupply = ConverterBigIntValueToLong(input.TotalSupply.Value),
                ExternalInfo = tokenExternalInfo
            };
            State.TokenContract.Create.Send(tokenCreateInput);

            var protocolInfo = new NFTProtocolInfo
            {
                Symbol = symbol,
                BaseUri = input.BaseUri,
                MaxCount = tokenCreateInput.TotalSupply,
                Creator = Context.Sender,
                Metadata = new Metadata {Value = {tokenExternalInfo.Value}},
                ProtocolName = input.ProtocolName,
                IsTokenIdReuse = input.IsTokenIdReuse
            };
            State.NftProtocolMap[symbol] = protocolInfo;

            Context.Fire(new NFTProtocolCreated
            {
                Symbol = tokenCreateInput.Symbol,
                Creator = tokenCreateInput.Issuer,
                IsBurnable = tokenCreateInput.IsBurnable,
                IssueChainId = tokenCreateInput.IssueChainId,
                ProtocolName = tokenCreateInput.TokenName,
                MaxCount = tokenCreateInput.TotalSupply,
                Metadata = protocolInfo.Metadata,
                BaseUri = protocolInfo.BaseUri
            });

            return new StringValue
            {
                Value = symbol
            };
        }

        public override Empty CrossChainCreate(CrossChainCreateInput input)
        {
            MakeSureTokenContractAddressSet();
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
            {
                Symbol = input.Symbol
            });
            if (tokenInfo == null)
            {
                throw new AssertionException($"Token info {input.Symbol} not exists.");
            }

            var maxCount = tokenInfo.ExternalInfo.Value[NftTotalSupplyMetadataKey];
            var baseUri = tokenInfo.ExternalInfo.Value[NftBaseUriMetadataKey];
            var isTokenIdReuse = bool.Parse(tokenInfo.ExternalInfo.Value[NftTokenIdReuseMetadataKey]);
            var nftTypeShortName = input.Symbol.Substring(2);
            var nftTypeFullName = State.NFTTypeFullNameMap[nftTypeShortName];
            if (nftTypeFullName == null)
            {
                throw new AssertionException(
                    $"Full name of {nftTypeShortName} not found. Use AddNFTType to add this new pair.");
            }

            var nftProtocolInfo = new NFTProtocolInfo
            {
                Symbol = input.Symbol,
                MaxCount = maxCount,
                BaseUri = baseUri,
                Creator = tokenInfo.Issuer,
                IsBurnable = tokenInfo.IsBurnable,
                IssueChainId = tokenInfo.IssueChainId,
                IsTokenIdReuse = isTokenIdReuse,
                Metadata = new Metadata {Value = {tokenInfo.ExternalInfo.Value}},
                ProtocolName = tokenInfo.TokenName,
                NftType = nftTypeFullName
            };
            State.NftProtocolMap[input.Symbol] = nftProtocolInfo;
            Context.Fire(new NFTProtocolCreated
            {
                Symbol = input.Symbol,
                Creator = nftProtocolInfo.Creator,
                IsBurnable = nftProtocolInfo.IsBurnable,
                IssueChainId = nftProtocolInfo.IssueChainId,
                ProtocolName = nftProtocolInfo.ProtocolName,
                MaxCount = nftProtocolInfo.MaxCount,
                Metadata = nftProtocolInfo.Metadata,
                BaseUri = nftProtocolInfo.BaseUri
            });
            return new Empty();
        }

        public override Empty AddNFTType(AddNFTTypeInput input)
        {
            AssertSenderIsParliamentDefaultAddress();
            Assert(input.ShortName.Length == 2, "Incorrect short name.");
            Assert(State.NFTTypeFullNameMap[input.ShortName] == null, $"Short name {input.ShortName} already exists.");
            Assert(State.NFTTypeShortNameMap[input.FullName] == null, $"Full name {input.FullName} already exists.");
            State.NFTTypeFullNameMap[input.ShortName] = input.FullName;
            State.NFTTypeShortNameMap[input.FullName] = input.ShortName;
            return new Empty();
        }

        public override Empty RemoveNFTType(StringValue input)
        {
            AssertSenderIsParliamentDefaultAddress();
            Assert(input.Value.Length == 2, "Incorrect short name.");
            var fullName = State.NFTTypeFullNameMap[input.Value];
            State.NFTTypeFullNameMap.Remove(input.Value);
            State.NFTTypeShortNameMap.Remove(fullName);
            return new Empty();
        }

        private void AssertSenderIsParliamentDefaultAddress()
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            if (State.ParliamentDefaultAddress.Value == null)
            {
                State.ParliamentDefaultAddress.Value =
                    State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            }

            Assert(Context.Sender == State.ParliamentDefaultAddress.Value, "No permission.");
        }

        private ExternalInfo GetTokenExternalInfo(CreateInput input)
        {
            var tokenExternalInfo = new ExternalInfo
            {
                Value = {input.Metadata.Value}
            };

            // Add NFT Type to external info.
            tokenExternalInfo.Value.Add(NftTypeMetadataKey, input.NftType.ToString());
            // Add Uri to external info.
            tokenExternalInfo.Value.Add(NftBaseUriMetadataKey, input.BaseUri);
            tokenExternalInfo.Value.Add(NftTokenIdReuseMetadataKey, input.IsTokenIdReuse.ToString());
            tokenExternalInfo.Value.Add(NftTotalSupplyMetadataKey, input.TotalSupply.Value);
            return tokenExternalInfo;
        }
    }
}