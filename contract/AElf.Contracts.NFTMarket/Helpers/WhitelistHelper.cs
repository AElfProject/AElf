using AElf.Contracts.Whitelist;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.NFTMarket.Helpers;

public static class WhitelistHelper
{
    internal static Hash CalculateProjectId(string symbol, long tokenId, Address sender)
    {
        return HashHelper.ComputeFrom($"{symbol}{tokenId}{sender}");
    }

    internal static Price DeserializedInfo(TagInfo tagInfo)
    {
        var deserializedInfo = new PriceTag();
        deserializedInfo.MergeFrom(tagInfo.Info);
        return new Price
        {
            Symbol = deserializedInfo.Symbol,
            Amount = deserializedInfo.Amount
        };
    }
}