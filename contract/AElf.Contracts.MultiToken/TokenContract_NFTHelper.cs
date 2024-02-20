using System.Linq;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    private SymbolType GetCreateInputSymbolType(string symbol)
    {
        var words = symbol.Split(TokenContractConstants.NFTSymbolSeparator);
        Assert(words[0].Length > 0 && words[0].All(IsValidCreateSymbolChar), "Invalid Symbol input");
        if (words.Length == 1) return SymbolType.Token;
        Assert(words.Length >= 2, "Invalid NFT Symbol input");
        for (var i = 1; i < words.Length; ++i)
        {
            var word = words[i];
            Assert(word.Length > 0 && word.All(IsValidItemIdChar), "Invalid NFT Symbol input");
        }
        // if we want to allow sub nft collection to be created with a different owner, we can check against words[^1]
        return words[1] == TokenContractConstants.CollectionSymbolSuffix ? SymbolType.NftCollection : SymbolType.Nft;
    }

    private void AssertNFTCreateInput(CreateInput input)
    {
        Assert(input.Decimals == 0, "NFT's decimals must be 0");
    }
}