using AElf.Contracts.NFT;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NFTMinter
{
    public partial class NFTMinterContract
    {
        public override Empty CreateBadge(CreateBadgeInput input)
        {
            CheckSymbolAndPermission(input.Symbol);
            var badgeInfo = State.BadgeInfoMap[input.Symbol][input.TokenId];
            if (badgeInfo != null)
            {
                throw new AssertionException("Badge already created.");
            }

            badgeInfo = new BadgeInfo
            {
                BadgeName = input.Alias,
                BadgeCreator = Context.Sender
            };
            State.BadgeInfoMap[input.Symbol][input.TokenId] = badgeInfo;

            var nftProtocol = ValidNFTProtocol(input.Symbol);
            var metadata = input.Metadata == null ? new Metadata() : new Metadata {Value = {input.Metadata.Value}};
            metadata.Value[BadgeNameMetadataKey] = input.Alias;
            State.NFTContract.Mint.Send(new MintInput
            {
                Symbol = input.Symbol,
                Alias = input.Alias,
                Metadata = metadata,
                Owner = input.Owner ?? nftProtocol.Creator,
                Quantity = 1,
                TokenId = input.TokenId,
                Uri = input.Uri
            });
            return new Empty();
        }

        public override Empty ConfigBadge(ConfigBadgeInput input)
        {
            CheckSymbolAndPermission(input.Symbol);
            var badgeInfo = State.BadgeInfoMap[input.Symbol][input.TokenId];
            if (badgeInfo == null)
            {
                throw new AssertionException("Badge not created.");
            }

            if (badgeInfo.StartTime != null)
            {
                // Cannot change badge info if minting period already started.
                Assert(badgeInfo.StartTime < Context.CurrentBlockTime, "Badge minting period already started.");
            }

            Assert(input.Limit > 0, "Invalid limit.");
            var startTime = badgeInfo.StartTime == null
                ? input.StartTime ?? Context.CurrentBlockTime
                : badgeInfo.StartTime;
            var endTime = badgeInfo.StartTime == null
                ? input.EndTime ?? Context.CurrentBlockTime.AddDays(100000)
                : badgeInfo.EndTime;

            State.BadgeInfoMap[input.Symbol][input.TokenId] = new BadgeInfo
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                StartTime = startTime,
                EndTime = endTime,
                IsPublic = input.IsPublic
            };

            State.MintLimitMap[input.Symbol][input.TokenId] = input.Limit;

            Context.Fire(new BadgeInfoChanged
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                IsPublic = input.IsPublic,
                Limit = input.Limit
            });
            return new Empty();
        }

        public override Empty ManageMintingWhiteList(ManageMintingWhiteListInput input)
        {
            CheckSymbolAndPermission(input.Symbol);
            var badgeInfo = State.BadgeInfoMap[input.Symbol][input.TokenId];
            Assert(!badgeInfo.IsPublic, "This badge is not whitelist only.");
            foreach (var address in input.AddressList.Value)
            {
                if (input.IsRemove)
                {
                    State.IsInWhiteListMap[input.Symbol][input.TokenId].Remove(address);
                }
                else
                {
                    State.IsInWhiteListMap[input.Symbol][input.TokenId][address] = true;
                }
            }

            Context.Fire(new MintingWhiteListChanged
            {
                Symbol = input.Symbol,
                TokenId = input.TokenId,
                IsRemove = input.IsRemove,
                AddressList = input.AddressList
            });

            return new Empty();
        }
    }
}