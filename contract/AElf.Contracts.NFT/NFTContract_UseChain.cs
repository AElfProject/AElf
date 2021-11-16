using System.Collections.Generic;
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
            var minterList = GetMinterList(tokenInfo);
            Assert(minterList.Value.Contains(Context.Sender), "No permission to mint.");
            Assert(tokenInfo.IssueChainId == Context.ChainId, "Incorrect chain.");
            var tokenHash = CalculateTokenHash(input.Symbol, input.TokenName);
            Assert(State.NftInfoMap[tokenHash] == null,
                $"{input.Symbol} NFT with name {input.TokenName} already minted.");

            var baseInfo = State.NftBaseInfoMap[input.Symbol];
            if (baseInfo == null)
            {
                throw new AssertionException($"Invalid NFT Token symbol: {input.Symbol}");
            }

            var nftMetadata = baseInfo.Metadata;
            foreach (var pair in input.Metadata.Value)
            {
                nftMetadata.Value.Add(pair.Key, pair.Value);
            }

            var nftInfo = new NFTInfo
            {
                Symbol = input.Symbol,
                Owner = input.Owner ?? Context.Sender,
                Uri = input.Uri,
                TokenName = input.TokenName,
                Number = baseInfo.MintedCount.Add(1),
                Creator = baseInfo.Creator,
                Metadata = nftMetadata,
                Minter = Context.Sender
            };
            State.NftInfoMap[tokenHash] = nftInfo;
            return tokenHash;
        }

        private Hash CalculateTokenHash(string symbol, string tokenName)
        {
            return HashHelper.ComputeFrom($"{symbol}{tokenName}");
        }

        public override Empty AddMinters(AddMintersInput input)
        {
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