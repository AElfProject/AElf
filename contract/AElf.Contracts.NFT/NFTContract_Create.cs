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
                Issuer = input.Creator ?? Context.Sender,
                IsBurnable = input.IsBurnable,
                IssueChainId = input.IssueChainId,
                TokenName = input.ProtocolName,
                TotalSupply = input.TotalSupply,
                ExternalInfo = tokenExternalInfo
            };
            State.TokenContract.Create.Send(tokenCreateInput);

            State.MinterListMap[symbol] = new MinterList
            {
                Value = {input.Creator}
            };

            var protocolInfo = new NFTProtocolInfo
            {
                Symbol = symbol,
                BaseUri = input.BaseUri,
                TotalSupply = tokenCreateInput.TotalSupply,
                Creator = tokenCreateInput.Issuer,
                Metadata = new Metadata {Value = {tokenExternalInfo.Value}},
                ProtocolName = tokenCreateInput.TokenName,
                IsTokenIdReuse = input.IsTokenIdReuse,
                IssueChainId = tokenCreateInput.IssueChainId,
                IsBurnable = tokenCreateInput.IsBurnable,
                NftType = input.NftType
            };
            State.NftProtocolMap[symbol] = protocolInfo;

            Context.Fire(new NFTProtocolCreated
            {
                Symbol = tokenCreateInput.Symbol,
                Creator = tokenCreateInput.Issuer,
                IsBurnable = tokenCreateInput.IsBurnable,
                IssueChainId = tokenCreateInput.IssueChainId,
                ProtocolName = tokenCreateInput.TokenName,
                TotalSupply = tokenCreateInput.TotalSupply,
                Metadata = protocolInfo.Metadata,
                BaseUri = protocolInfo.BaseUri,
                IsTokenIdReuse = protocolInfo.IsTokenIdReuse
            });

            return new StringValue
            {
                Value = symbol
            };
        }

        public override Empty CrossChainCreate(CrossChainCreateInput input)
        {
            MakeSureTokenContractAddressSet();
            InitialNFTTypeNameMap();
            Assert(State.NftProtocolMap[input.Symbol] == null, $"Protocol {input.Symbol} already created.");
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
            {
                Symbol = input.Symbol
            });
            if (string.IsNullOrEmpty(tokenInfo.Symbol))
            {
                throw new AssertionException($"Token info {input.Symbol} not exists.");
            }

            var baseUri = tokenInfo.ExternalInfo.Value[NftBaseUriMetadataKey];
            var isTokenIdReuse = bool.Parse(tokenInfo.ExternalInfo.Value[NftTokenIdReuseMetadataKey]);
            var nftTypeShortName = input.Symbol.Substring(0, 2);
            var nftTypeFullName = State.NFTTypeFullNameMap[nftTypeShortName];
            if (nftTypeFullName == null)
            {
                throw new AssertionException(
                    $"Full name of {nftTypeShortName} not found. Use AddNFTType to add this new pair.");
            }

            var nftProtocolInfo = new NFTProtocolInfo
            {
                Symbol = input.Symbol,
                TotalSupply = tokenInfo.TotalSupply,
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

            State.MinterListMap[input.Symbol] = new MinterList
            {
                Value = {nftProtocolInfo.Creator}
            };

            Context.Fire(new NFTProtocolCreated
            {
                Symbol = input.Symbol,
                Creator = nftProtocolInfo.Creator,
                IsBurnable = nftProtocolInfo.IsBurnable,
                IssueChainId = nftProtocolInfo.IssueChainId,
                ProtocolName = nftProtocolInfo.ProtocolName,
                TotalSupply = nftProtocolInfo.TotalSupply,
                Metadata = nftProtocolInfo.Metadata,
                BaseUri = nftProtocolInfo.BaseUri,
                IsTokenIdReuse = isTokenIdReuse
            });
            return new Empty();
        }

        public override Empty AddNFTType(AddNFTTypeInput input)
        {
            AssertSenderIsParliamentDefaultAddress();
            InitialNFTTypeNameMap();
            var fullName = input.FullName;
            Assert(input.ShortName.Length == 2, "Incorrect short name.");
            Assert(State.NFTTypeFullNameMap[input.ShortName] == null, $"Short name {input.ShortName} already exists.");
            Assert(State.NFTTypeShortNameMap[fullName] == null, $"Full name {fullName} already exists.");
            State.NFTTypeFullNameMap[input.ShortName] = fullName;
            State.NFTTypeShortNameMap[fullName] = input.ShortName;
            var nftTypes = State.NFTTypes.Value;
            nftTypes.Value.Add(input.ShortName, fullName);
            State.NFTTypes.Value = nftTypes;
            return new Empty();
        }

        public override Empty RemoveNFTType(StringValue input)
        {
            AssertSenderIsParliamentDefaultAddress();
            InitialNFTTypeNameMap();
            Assert(input.Value.Length == 2, "Incorrect short name.");
            Assert(State.NFTTypeFullNameMap[input.Value] != null, $"Short name {input.Value} does not exist.");
            var fullName = State.NFTTypeFullNameMap[input.Value];
            State.NFTTypeFullNameMap.Remove(input.Value);
            State.NFTTypeShortNameMap.Remove(fullName);
            var nftTypes = State.NFTTypes.Value;
            nftTypes.Value.Remove(input.Value);
            State.NFTTypes.Value = nftTypes;
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
            tokenExternalInfo.Value[NftTypeMetadataKey] = input.NftType;
            // Add Uri to external info.
            tokenExternalInfo.Value[NftBaseUriMetadataKey] = input.BaseUri;
            tokenExternalInfo.Value[NftTokenIdReuseMetadataKey] = input.IsTokenIdReuse.ToString();
            return tokenExternalInfo;
        }
    }
}