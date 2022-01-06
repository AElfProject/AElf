using AElf.Contracts.NFT;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMinter
{
    public partial class NFTMinterContract
    {
        public override Empty MintBadge(MintBadgeInput input)
        {
            var badgeInfo = State.BadgeInfoMap[input.Symbol][input.TokenId];
            if (badgeInfo?.BadgeName == null)
            {
                throw new AssertionException("Badge info not exists.");
            }

            var limit = State.MintLimitMap[input.Symbol][input.TokenId];
            Assert(limit > 0, "Minting of this badge not started.");

            // Check the limit.
            var minted = State.MintedMap[input.Symbol][input.TokenId];
            Assert(minted < limit, $"Reached the minting limit {limit}");

            var owner = input.Owner ?? Context.Sender;
            // Check whether owner in whitelist.
            if (!badgeInfo.IsPublic)
            {
                Assert(State.IsInWhiteListMap[input.Symbol][input.TokenId][owner],
                    $"{owner} is not in the white list.");
            }

            State.NFTContract.Mint.Send(new MintInput
            {
                Symbol = input.Symbol,
                Owner = owner,
                Quantity = 1,
                TokenId = input.TokenId
            });

            State.MintedMap[input.Symbol][input.TokenId] = minted.Add(1);
            return new Empty();
        }
    }
}