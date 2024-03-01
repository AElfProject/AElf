using System.Linq;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    private SymbolType GetCreateInputSymbolType(string symbol)
    {
        var splitSymbols = symbol.Split(TokenContractConstants.NFTSymbolSeparator);
        
        AssertTokenSymbolIsValid(splitSymbols);
            
        if (splitSymbols.Length == 1) return SymbolType.Token;

        AssertNFTSymbolIsValid(splitSymbols);
        
        // if we want to allow sub nft collection to be created with a different owner, we can check against words[^1]
        return splitSymbols[1] == TokenContractConstants.CollectionSymbolSuffix ? SymbolType.NftCollection : SymbolType.Nft;
    }

    private void AssertSymbolIsValid(string symbol)
    {
        var splitSymbols = symbol.Split(TokenContractConstants.NFTSymbolSeparator);
        AssertTokenSymbolIsValid(splitSymbols);
        if (splitSymbols.Length == 1) return;
        AssertNFTSymbolIsValid(splitSymbols);
    }

    private void AssertTokenSymbolIsValid(string symbol)
    {
        var splitSymbols = symbol.Split(TokenContractConstants.NFTSymbolSeparator);
        AssertTokenSymbolIsValid(splitSymbols);
    }

    private void AssertTokenSymbolIsValid(string[] splitSymbols)
    {
        Assert(splitSymbols[0].Length > 0 && splitSymbols[0].All(IsValidCreateSymbolChar), "Invalid Symbol input");
    }

    private void AssertNFTSymbolIsValid(string symbol)
    {
        var splitSymbols = symbol.Split(TokenContractConstants.NFTSymbolSeparator);
        AssertNFTSymbolIsValid(splitSymbols);
    }
    
    private void AssertNFTSymbolIsValid(string[] splitSymbols)
    {
        Assert(splitSymbols.Length >= 2, "Invalid NFT Symbol input");
        Assert(splitSymbols[1].Length > 0 && splitSymbols[1].All(IsValidItemIdChar), "Invalid NFT Symbol input");
        for (var i = 2; i < splitSymbols.Length; ++i)
        {
            var word = splitSymbols[i];
            Assert(word.Length > 0 && word[0] != '0' && word.All(IsValidItemIdChar), "Invalid NFT Symbol input");
        }
    }

    private void AssertNFTCreateInput(CreateInput input)
    {
        Assert(input.Decimals == 0, "NFT's decimals must be 0");
    }
}