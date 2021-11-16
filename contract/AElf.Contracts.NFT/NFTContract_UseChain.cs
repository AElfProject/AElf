using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFT
{
    public partial class NFTContract
    {
        public override Hash Mint(MintInput input)
        {
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
            {
                Symbol = input.Symbol
            });
            var protocolInfo = State.NftProtocolMap[input.Symbol];
            if (protocolInfo == null)
            {
                throw new AssertionException($"Invalid NFT Token symbol: {input.Symbol}");
            }

            var tokenId = protocolInfo.MintedCount.Add(1);
            var minterList = GetMinterList(tokenInfo);
            Assert(minterList.Value.Contains(Context.Sender), "No permission to mint.");
            Assert(tokenInfo.IssueChainId == Context.ChainId, "Incorrect chain.");
            var tokenHash = CalculateTokenHash(input.Symbol, tokenId);

            var nftMetadata = protocolInfo.Metadata;
            foreach (var pair in input.Metadata.Value)
            {
                nftMetadata.Value.Add(pair.Key, pair.Value);
            }

            var nftInfo = new NFTInfo
            {
                Symbol = input.Symbol,
                Owner = input.Owner ?? Context.Sender,
                BaseUri = protocolInfo.BaseUri,
                Uri = input.Uri,
                TokenName = tokenInfo.TokenName,
                TokenId = tokenId,
                Creator = protocolInfo.Creator,
                Metadata = nftMetadata,
                Minter = Context.Sender
            };
            State.NftInfoMap[tokenHash] = nftInfo;
            return tokenHash;
        }

        public override Empty Transfer(TransferInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftInfo = State.NftInfoMap[tokenHash];
            Assert(nftInfo.Owner == Context.Sender, "No permission.");
            nftInfo.Owner = input.To;
            State.NftInfoMap[tokenHash] = nftInfo;
            return new Empty();
        }

        public override Empty TransferFrom(TransferFromInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftInfo = State.NftInfoMap[tokenHash];
            // Need to make sure this nft is still owned by the From address
            Assert(nftInfo.Owner == input.From && State.IsApprovedMap[tokenHash][input.From][Context.Sender],
                "No permission.");
            nftInfo.Owner = input.To;
            State.NftInfoMap[tokenHash] = nftInfo;
            return new Empty();
        }

        public override Empty Approve(ApproveInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftInfo = State.NftInfoMap[tokenHash];
            Assert(nftInfo.Owner == Context.Sender, "No permission.");
            State.IsApprovedMap[tokenHash][Context.Sender][input.Spender] = true;
            return new Empty();
        }

        public override Empty UnApprove(UnApproveInput input)
        {
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenId);
            var nftInfo = State.NftInfoMap[tokenHash];
            Assert(nftInfo.Owner == Context.Sender, "No permission.");
            State.IsApprovedMap[tokenHash][Context.Sender].Remove(input.Spender);
            return new Empty();
        }

        private Hash CalculateTokenHash(string symbol, long tokenId)
        {
            return HashHelper.ComputeFrom($"{symbol}{tokenId}");
        }

        public override Empty AddMinters(AddMintersInput input)
        {
            var protocolInfo = State.NftProtocolMap[input.Symbol];
            Assert(Context.Sender == protocolInfo.Creator, "No permission.");
            foreach (var minter in input.MinterList.Value)
            {
                if (!protocolInfo.MinterList.Value.Contains(minter))
                {
                    protocolInfo.MinterList.Value.Add(minter);
                }
            }

            State.NftProtocolMap[input.Symbol] = protocolInfo;
            return new Empty();
        }

        public override Empty RemoveMiners(RemoveMinersInput input)
        {
            var protocolInfo = State.NftProtocolMap[input.Symbol];
            Assert(Context.Sender == protocolInfo.Creator, "No permission.");
            foreach (var minter in input.MinterList.Value)
            {
                if (protocolInfo.MinterList.Value.Contains(minter))
                {
                    protocolInfo.MinterList.Value.Remove(minter);
                }
            }

            State.NftProtocolMap[input.Symbol] = protocolInfo;
            return new Empty();
        }

        private MinterList GetMinterList(TokenInfo tokenInfo)
        {
            var minterList = State.MinterListMap[tokenInfo.Symbol] ?? new MinterList();
            if (!minterList.Value.Contains(tokenInfo.Issuer))
            {
                minterList.Value.Add(tokenInfo.Issuer);
            }

            return minterList;
        }
    }
}