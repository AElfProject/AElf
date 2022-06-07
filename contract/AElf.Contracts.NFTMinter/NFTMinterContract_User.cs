using AElf.Contracts.NFT;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
using TransferFromInput = AElf.Contracts.MultiToken.TransferFromInput;

namespace AElf.Contracts.NFTMinter;

public partial class NFTMinterContract
{
    public override Empty MintBadge(MintBadgeInput input)
    {
        var badgeInfo = State.BadgeInfoMap[input.Symbol][input.TokenId];
        if (badgeInfo?.BadgeName == null) throw new AssertionException("Badge info not exists.");

        var limit = State.MintLimitMap[input.Symbol][input.TokenId];
        Assert(limit > 0, "Minting of this badge not started.");

        // Check the limit.
        var minted = State.MintedMap[input.Symbol][input.TokenId];
        Assert(minted < limit, $"Reached the minting limit {limit}");

        var owner = input.Owner ?? Context.Sender;
        // Check whether owner in whitelist.
        if (!badgeInfo.IsPublic)
            Assert(State.IsInWhiteListMap[input.Symbol][input.TokenId][owner],
                $"{owner} is not in the white list.");

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

    public override Empty Unbox(UnboxInput input)
    {
        var blindBoxInfo = State.BlindBoxInfoMap[input.Symbol][input.Index];
        if (blindBoxInfo == null) throw new AssertionException($"Index {input.Index} not existed.");

        PayForBlindBox(blindBoxInfo);

        var weightVector = State.BlindBoxWeightVectorMap[input.Symbol][input.Index];
        var totalWeights = blindBoxInfo.TotalWeights;

        var randomBytes = State.RandomNumberProviderContract.GetRandomBytes.Call(new Int64Value
        {
            Value = Context.CurrentHeight.Sub(1)
        }.ToBytesValue());
        var randomHash =
            HashHelper.ConcatAndCompute(Context.PreviousBlockHash, HashHelper.ComputeFrom(randomBytes));

        NFTTemplate useTemplate;
        do
        {
            var randomNumber = Context.ConvertHashToInt64(randomHash, 0, totalWeights);
            var blindBoxIndex = 0;
            for (var i = 0; i < weightVector.Value.Count; i++)
            {
                blindBoxIndex = i;
                if (randomNumber > weightVector.Value[i]) break;
            }

            useTemplate = blindBoxInfo.TemplateList.Value[blindBoxIndex];
            randomHash = HashHelper.ComputeFrom(randomHash);
        } while (useTemplate.MintedQuantity > useTemplate.Quantity);

        State.NFTContract.Mint.Send(new MintInput
        {
            Symbol = useTemplate.Symbol,
            TokenId = useTemplate.TokenId,
            Alias = useTemplate.Alias,
            Owner = Context.Sender,
            Metadata = new Metadata { Value = { useTemplate.Metadata.Value } },
            Quantity = 1,
            Uri = useTemplate.Uri
        });

        if (!blindBoxInfo.IsTokenIdFixed) useTemplate.TokenId = useTemplate.TokenId.Add(1);

        useTemplate.MintedQuantity = useTemplate.MintedQuantity.Add(1);

        State.BlindBoxInfoMap[input.Symbol][input.Index] = blindBoxInfo;

        return new Empty();
    }

    private void PayForBlindBox(BlindBoxInfo blindBoxInfo)
    {
        var costReceiver = blindBoxInfo.CostReceiver;
        if (blindBoxInfo.CostTokenId == 0)
        {
            // User fungible tokens.
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = costReceiver ?? GetNFTProtocolCreator(blindBoxInfo.Symbol),
                Symbol = blindBoxInfo.CostSymbol,
                Amount = blindBoxInfo.CostAmount
            });
        }
        else
        {
            if (costReceiver == null)
            {
                // Burn cost nft.

                State.NFTContract.TransferFrom.Send(new NFT.TransferFromInput
                {
                    From = Context.Sender,
                    TokenId = blindBoxInfo.CostTokenId,
                    To = Context.Self,
                    Symbol = blindBoxInfo.CostSymbol,
                    Amount = blindBoxInfo.CostAmount
                });
                State.NFTContract.Burn.Send(new BurnInput
                {
                    Symbol = blindBoxInfo.CostSymbol,
                    TokenId = blindBoxInfo.CostTokenId,
                    Amount = blindBoxInfo.CostAmount
                });
            }
            else
            {
                State.NFTContract.TransferFrom.Send(new NFT.TransferFromInput
                {
                    From = Context.Sender,
                    TokenId = blindBoxInfo.CostTokenId,
                    To = blindBoxInfo.CostReceiver,
                    Symbol = blindBoxInfo.CostSymbol,
                    Amount = blindBoxInfo.CostAmount
                });
            }
        }
    }
}