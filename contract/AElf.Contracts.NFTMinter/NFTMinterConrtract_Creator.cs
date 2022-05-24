using System.Linq;
using AElf.Contracts.NFT;
using AElf.CSharp.Core;
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

        public override Empty Box(BoxInput input)
        {
            CheckSymbolAndPermission(input.Symbol);
            Assert(State.BlindBoxInfoMap[input.Symbol][input.Index] == null, $"Index {input.Index} already existed.");
            var templateList = new NFTTemplateList
            {
                Value = {input.TemplateList.Value.OrderBy(t => t.TokenId)}
            };
            Assert(templateList.Value.All(t => t.Quantity > 0), "Incorrect quantity.");
            Assert(templateList.Value.All(t => t.Weight > 0), "Incorrect weight.");
            CheckTemplateTokenIds(templateList, input.IsTokenIdFixed, out var startTokenId, out var endTokenId);
            var blindBoxInfo = new BlindBoxInfo
            {
                Symbol = input.Symbol,
                TemplateList = templateList,
                IsTokenIdFixed = input.IsTokenIdFixed,
                CostSymbol = input.CostSymbol,
                CostTokenId = input.CostTokenId,
                CostAmount = input.CostAmount,
                CostReceiver = input.CostReceiver,
                StartTokenId = startTokenId,
                SupposedEndTokenId = endTokenId,
                TotalWeights = templateList.Value.Sum(t => t.Weight)
            };
            State.BlindBoxInfoMap[input.Symbol][input.Index] = blindBoxInfo;
            var vector = ConstructWeightVector(templateList);
            Assert(blindBoxInfo.TotalWeights == vector.Value.Last(), "Something wrong with weight.");
            State.BlindBoxWeightVectorMap[input.Symbol][input.Index] = vector;
            Context.Fire(new BlindBoxForged
            {
                Symbol = blindBoxInfo.Symbol,
                TemplateList = templateList,
                IsTokenIdFixed = blindBoxInfo.IsTokenIdFixed,
                CostSymbol = blindBoxInfo.CostSymbol,
                CostTokenId = blindBoxInfo.CostTokenId,
                CostAmount = blindBoxInfo.CostAmount,
                CostReceiver = blindBoxInfo.CostReceiver,
                StartTokenId = startTokenId,
                SupposedEndTokenId = endTokenId,
            });
            return new Empty();
        }

        private void CheckTemplateTokenIds(NFTTemplateList templateList, bool isTokenIdFixed, out long startTokenId,
            out long endTokenId)
        {
            if (isTokenIdFixed)
            {
                startTokenId = templateList.Value.First().TokenId;
                endTokenId = startTokenId.Add(1);
                var checkTokenId = startTokenId;
                foreach (var template in templateList.Value.Skip(1))
                {
                    Assert(template.TokenId >= checkTokenId,
                        $"{template.Alias} cannot start from token id {checkTokenId}");
                    checkTokenId = template.TokenId;
                    endTokenId = checkTokenId;
                }
            }
            else
            {
                var firstTemplate = templateList.Value.First();
                startTokenId = firstTemplate.TokenId;
                var checkTokenId = startTokenId.Add(firstTemplate.Quantity).Sub(1);
                endTokenId = checkTokenId;
                foreach (var template in templateList.Value.Skip(1))
                {
                    Assert(template.TokenId > checkTokenId,
                        $"{template.Alias} cannot start from token id {checkTokenId}");
                    checkTokenId = template.TokenId.Add(template.Quantity).Sub(1);
                    endTokenId = checkTokenId;
                }
            }
        }

        private Int64List ConstructWeightVector(NFTTemplateList templateList)
        {
            var weightList = templateList.Value.OrderBy(t => t.Weight).Select(t => t.Weight).ToList();
            var vector = new Int64List
            {
                Value = {weightList.First()}
            };
            foreach (var weight in weightList.Skip(1))
            {
                var lastValue = vector.Value.Last();
                vector.Value.Add(lastValue.Add(weight));
            }

            return vector;
        }
    }
}