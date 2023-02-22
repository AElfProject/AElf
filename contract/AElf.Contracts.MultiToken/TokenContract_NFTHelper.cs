using System.Linq;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    public enum SymbolType
    {
        NFTCollection,
        NFT,
        TOKEN
    }

    private SymbolType GetCreateInputSymbolType(string symbol)
    {
        var words = symbol.Split(TokenContractConstants.NFTSymbolSeparator);
        Assert(words[0].Length > 0 && words[0].All(IsValidCreateSymbolChar), "Invalid Symbol input");
        if (words.Length == 1) return SymbolType.TOKEN;
        Assert(words.Length == 2 && words[1].Length > 0 && words[1].All(IsValidItemIdChar), "Invalid NFT Symbol input");
        return words[1] == "0" ? SymbolType.NFTCollection : SymbolType.NFT;
    }

    private void AssertNFTCreateInput(CreateInput input)
    {
        Assert(input.Decimals == 0, "NFT's decimals must be 0");
    }
}