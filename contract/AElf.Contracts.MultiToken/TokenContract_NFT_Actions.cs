using System.Linq;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    private Empty CreateNFTCollection(CreateInput input)
    {
        AssertNFTCreateInput(input);
        return CreateToken(input, SymbolType.NftCollection);
    }

    private Empty CreateNFTInfo(CreateInput input)
    {
        AssertNFTCreateInput(input);
        var nftCollectionInfo = AssertNftCollectionExist(input.Symbol);
        input.IssueChainId = input.IssueChainId == 0 ? nftCollectionInfo.IssueChainId : input.IssueChainId;
        Assert(input.IssueChainId == nftCollectionInfo.IssueChainId,
            "NFT create ChainId must be collection's issue chainId");
        Assert(Context.Sender == nftCollectionInfo.Issuer && nftCollectionInfo.Issuer == input.Issuer,
            "NFT issuer must be collection's issuer");
        if (nftCollectionInfo.Symbol == TokenContractConstants.SeedCollectionSymbol)
        {
            Assert(
                input.ExternalInfo.Value.TryGetValue(TokenContractConstants.SeedOwnedSymbolExternalInfoKey,
                    out var ownedSymbol), "OwnedSymbol does not exist.");
            Assert(input.ExternalInfo.Value.TryGetValue(TokenContractConstants.SeedExpireTimeExternalInfoKey,
                       out var expirationTime)
                   && long.TryParse(expirationTime, out var expirationTimeLong) &&
                   Context.CurrentBlockTime.Seconds <= expirationTimeLong, "Invalid ownedSymbol.");
            var ownedSymbolType = GetCreateInputSymbolType(ownedSymbol);
            Assert(ownedSymbolType != SymbolType.Nft, "Invalid OwnedSymbol.");
            CheckSymbolLength(ownedSymbol, ownedSymbolType);
            CheckTokenAndCollectionExists(ownedSymbol);
            CheckSymbolSeed(ownedSymbol);
            State.SymbolSeedMap[ownedSymbol] = input.Symbol;
        }

        return CreateToken(input, SymbolType.Nft);
    }

    private void CheckSymbolSeed(string ownedSymbol)
    {
        var oldSymbolSeed = State.SymbolSeedMap[ownedSymbol];

        Assert(oldSymbolSeed == null || !State.TokenInfos[oldSymbolSeed].ExternalInfo.Value
                   .TryGetValue(TokenContractConstants.SeedExpireTimeExternalInfoKey,
                       out var oldSymbolSeedExpireTime) ||
               !long.TryParse(oldSymbolSeedExpireTime, out var symbolSeedExpireTime)
               || Context.CurrentBlockTime.Seconds > symbolSeedExpireTime,
            "OwnedSymbol has been created");
    }


    private void DoTransferFrom(Address from, Address to, Address spender, string symbol, long amount, string memo)
    {
        // First check allowance.
        var allowance = State.Allowances[from][spender][symbol];
        if (allowance < amount)
        {
            if (IsInWhiteList(new IsInWhiteListInput { Symbol = symbol, Address = spender }).Value)
            {
                DoTransfer(from, to, symbol, amount, memo);
                DealWithExternalInfoDuringTransfer(new TransferFromInput()
                    { From = from, To = to, Symbol = symbol, Amount = amount, Memo = memo });
                return;
            }

            Assert(false,
                $"[TransferFrom]Insufficient allowance. Token: {symbol}; {allowance}/{amount}.\n" +
                $"From:{from}\tSpender:{spender}\tTo:{to}");
        }

        DoTransfer(from, to, symbol, amount, memo);
        DealWithExternalInfoDuringTransfer(new TransferFromInput()
            { From = from, To = to, Symbol = symbol, Amount = amount, Memo = memo });
        State.Allowances[from][spender][symbol] = allowance.Sub(amount);
    }


    private string GetNftCollectionSymbol(string inputSymbol)
    {
        var symbol = inputSymbol;
        var words = symbol.Split(TokenContractConstants.NFTSymbolSeparator);
        const int tokenSymbolLength = 1;
        if (words.Length == tokenSymbolLength) return null;
        Assert(words.Length == 2 && words[1].All(IsValidItemIdChar), "Invalid NFT Symbol Input");
        return symbol == $"{words[0]}-0" ? null : $"{words[0]}-0";
    }

    private TokenInfo AssertNftCollectionExist(string symbol)
    {
        var collectionSymbol = GetNftCollectionSymbol(symbol);
        if (collectionSymbol == null) return null;
        var collectionInfo = State.TokenInfos[collectionSymbol];
        Assert(collectionInfo != null, "NFT collection not exist");
        return collectionInfo;
    }
}