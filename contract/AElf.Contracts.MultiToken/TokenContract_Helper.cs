using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS0;
using AElf.Standards.ACS7;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    private static bool IsValidSymbol(string symbol)
    {
        return Regex.IsMatch(symbol, "^[a-zA-Z0-9]+(-[0-9]+)?$");
    }

    private bool IsValidItemId(string symbolItemId)
    {
        return Regex.IsMatch(symbolItemId, "^[0-9]+$");
    }

    private bool IsValidCreateSymbol(string symbol)
    {
        return Regex.IsMatch(symbol, "^[a-zA-Z0-9]+$");
    }

    private TokenInfo AssertValidToken(string symbol, long amount)
    {
        AssertValidSymbolAndAmount(symbol, amount);
        var tokenInfo = GetTokenInfo(symbol);
        Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol), $"Token is not found. {symbol}");
        return tokenInfo;
    }

    private void AssertValidApproveTokenAndAmount(string symbol, long amount)
    {
        Assert(amount > 0, "Invalid amount.");
        AssertApproveToken(symbol);
    }

    private void ValidTokenExists(string symbol)
    {
        var tokenInfo = State.TokenInfos[symbol];
        Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol),
            $"Token is not found. {symbol}");
    }
    
    private void AssertApproveToken(string symbol)
    {
        Assert(!string.IsNullOrEmpty(symbol), "Symbol can not be null.");
        var words = symbol.Split(TokenContractConstants.NFTSymbolSeparator);
        var symbolPrefix = words[0];
        var allSymbolIdentifier = GetAllSymbolIdentifier();
        Assert(symbolPrefix.Length > 0 && (IsValidCreateSymbol(symbolPrefix) || symbolPrefix.Equals(allSymbolIdentifier)), "Invalid symbol.");
        if (words.Length == 1)
        {
            if (!symbolPrefix.Equals(allSymbolIdentifier))
            {
                ValidTokenExists(symbolPrefix);
            }
            return;
        }
        Assert(words.Length == 2, "Invalid symbol length.");
        var itemId = words[1];
        Assert(itemId.Length > 0 && (IsValidItemId(itemId) || itemId.Equals(allSymbolIdentifier)), "Invalid NFT Symbol.");
        var nftSymbol = itemId.Equals(allSymbolIdentifier) ? GetCollectionSymbol(symbolPrefix) : symbol;
        ValidTokenExists(nftSymbol);
    }
    
    private string GetCollectionSymbol(string symbolPrefix)
    {
        return $"{symbolPrefix}-{TokenContractConstants.CollectionSymbolSuffix}";
    }

    private void AssertValidSymbolAndAmount(string symbol, long amount)
    {
        Assert(!string.IsNullOrEmpty(symbol) && IsValidSymbol(symbol),
            "Invalid symbol.");
        Assert(amount > 0, "Invalid amount.");
    }

    private void AssertValidMemo(string memo)
    {
        Assert(memo == null || Encoding.UTF8.GetByteCount(memo) <= TokenContractConstants.MemoMaxLength,
            "Invalid memo size.");
    }

    private void AssertValidInputAddress(Address input)
    {
        Assert(input != null && !input.Value.IsNullOrEmpty(), "Invalid input address.");
    }

    private void DoTransfer(Address from, Address to, string symbol, long amount, string memo = null)
    {
        Assert(from != to, "Can't do transfer to sender itself.");
        AssertValidMemo(memo);
        ModifyBalance(from, symbol, -amount);
        ModifyBalance(to, symbol, amount);
        Context.Fire(new Transferred
        {
            From = from,
            To = to,
            Symbol = symbol,
            Amount = amount,
            Memo = memo ?? string.Empty
        });
    }

    private void ModifyBalance(Address address, string symbol, long addAmount)
    {
        var before = GetBalance(address, symbol);
        if (addAmount < 0 && before < -addAmount)
            Assert(false,
                $"{address}. Insufficient balance of {symbol}. Need balance: {-addAmount}; Current balance: {before}");

        var target = before.Add(addAmount);
        State.Balances[address][symbol] = target;
    }

    private void ModifyFreeFeeAllowanceAmount(Address fromAddress,
        TransactionFeeFreeAllowancesMap transactionFeeFreeAllowancesMap, string symbol,
        long addAmount)
    {
        var freeAllowanceAmount = GetFreeFeeAllowanceAmount(transactionFeeFreeAllowancesMap, symbol);
        if (addAmount < 0 && freeAllowanceAmount < -addAmount)
        {
            Assert(false,
                $"Insufficient amount of {symbol} for free fee allowance. Need amount: {-addAmount}; Current amount: {freeAllowanceAmount}");
        }

        // Sort symbols by expiration time
        var symbolList = GetSymbolListSortedByExpirationTime(transactionFeeFreeAllowancesMap, fromAddress);

        foreach (var s in symbolList)
        {
            if (addAmount >= 0) break;
            
            if (!transactionFeeFreeAllowancesMap.Map[s].Map.ContainsKey(symbol)) continue;
            
            var currentAllowance = transactionFeeFreeAllowancesMap.Map[s].Map[symbol].Amount;

            if (currentAllowance == 0) continue;

            addAmount += currentAllowance;

            transactionFeeFreeAllowancesMap.Map[s].Map[symbol].Amount = addAmount >= 0 ? addAmount : 0;
        }
    }

    private List<string> GetSymbolListSortedByExpirationTime(TransactionFeeFreeAllowancesMap transactionFeeFreeAllowancesMap,
        Address fromAddress)
    {
        return transactionFeeFreeAllowancesMap.Map.Keys.OrderBy(t =>
            State.TransactionFeeFreeAllowancesConfigMap[t].RefreshSeconds - (Context.CurrentBlockTime -
                                                                        State.TransactionFeeFreeAllowancesLastRefreshTimes[
                                                                            fromAddress][t]).Seconds).ToList();
    }

    private long GetBalance(Address address, string symbol)
    {
        AssertValidInputAddress(address);
        var actualSymbol = GetActualTokenSymbol(symbol);
        Assert(!string.IsNullOrWhiteSpace(actualSymbol), "Invalid symbol.");
        return State.Balances[address][actualSymbol];
    }

    // private MethodFeeFreeAllowance GetFreeFeeAllowance(MethodFeeFreeAllowances freeAllowances, string symbol)
    // {
    //     return freeAllowances?.Value.FirstOrDefault(a => a.Symbol == symbol);
    // }

    private long GetFreeFeeAllowanceAmount(TransactionFeeFreeAllowancesMap transactionFeeFreeAllowancesMap, string symbol)
    {
        var allowance = 0L;
        var map = transactionFeeFreeAllowancesMap.Map;

        if (map == null) return allowance;

        foreach (var freeAllowances in map.Values)
        {
            freeAllowances.Map.TryGetValue(symbol, out var freeAllowance);

            allowance = allowance.Add(freeAllowance?.Amount ?? 0L);
        }

        return allowance;
    }

    private void AssertSystemContractOrLockWhiteListAddress(string symbol)
    {
        var symbolState = State.LockWhiteLists[symbol];
        var isInWhiteList = symbolState != null && symbolState[Context.Sender];
        var systemContractAddresses = Context.GetSystemContractNameToAddressMapping().Values;
        var isSystemContractAddress = systemContractAddresses.Contains(Context.Sender);
        Assert(isInWhiteList || isSystemContractAddress, "No Permission.");
    }

    private Address ExtractTokenContractAddress(ByteString bytes)
    {
        var validateSystemContractAddressInput = ValidateSystemContractAddressInput.Parser.ParseFrom(bytes);
        var validatedAddress = validateSystemContractAddressInput.Address;
        var validatedContractHashName = validateSystemContractAddressInput.SystemContractHashName;

        Assert(validatedContractHashName == SmartContractConstants.TokenContractSystemHashName,
            "Address validation failed.");
        return validatedAddress;
    }

    private void AssertCrossChainTransaction(Transaction originalTransaction, Address validAddress,
        params string[] validMethodNames)
    {
        var validateResult = validMethodNames.Contains(originalTransaction.MethodName)
                             && originalTransaction.To == validAddress;
        Assert(validateResult, "Invalid transaction.");
    }

    private void RegisterTokenInfo(TokenInfo tokenInfo)
    {
        Assert(!string.IsNullOrEmpty(tokenInfo.Symbol) && IsValidSymbol(tokenInfo.Symbol),
            "Invalid symbol.");
        Assert(!string.IsNullOrEmpty(tokenInfo.TokenName), "Token name can neither be null nor empty.");
        Assert(tokenInfo.TotalSupply > 0, "Invalid total supply.");
        Assert(tokenInfo.Issuer != null, "Invalid issuer address.");
        Assert(tokenInfo.Owner != null, "Invalid owner address.");
        State.TokenInfos[tokenInfo.Symbol] = tokenInfo;
        State.InsensitiveTokenExisting[tokenInfo.Symbol.ToUpper()] = true;
    }

    private void CrossChainVerify(Hash transactionId, long parentChainHeight, int chainId, MerklePath merklePath)
    {
        var verificationInput = new VerifyTransactionInput
        {
            TransactionId = transactionId,
            ParentChainHeight = parentChainHeight,
            VerifiedChainId = chainId,
            Path = merklePath
        };
        var address = Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);

        var verificationResult = Context.Call<BoolValue>(address,
            nameof(ACS7Container.ACS7ReferenceState.VerifyTransaction), verificationInput);
        Assert(verificationResult.Value, "Cross chain verification failed.");
    }

    private AuthorityInfo GetCrossChainTokenContractRegistrationController()
    {
        var parliamentContractAddress =
            Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
        var controller = new AuthorityInfo
        {
            ContractAddress = State.ParliamentContract.Value,
            OwnerAddress = Context.Call<Address>(parliamentContractAddress,
                nameof(ParliamentContractContainer.ParliamentContractReferenceState.GetDefaultOrganizationAddress),
                new Empty())
        };
        return controller;
    }

    private int GetIssueChainId(string symbol)
    {
        var tokenInfo = GetTokenInfo(symbol);
        return tokenInfo.IssueChainId;
    }

    private void AssertValidCreateInput(CreateInput input, SymbolType symbolType)
    {
        Assert(input.TokenName.Length <= TokenContractConstants.TokenNameLength
               && input.Symbol.Length > 0
               && input.Decimals >= 0
               && input.Decimals <= TokenContractConstants.MaxDecimals, "Invalid input.");

        CheckSymbolLength(input.Symbol, symbolType);
        if (symbolType == SymbolType.Nft) return;
        CheckTokenAndCollectionExists(input.Symbol);
        if (IsAddressInCreateWhiteList(Context.Sender)) CheckSymbolSeed(input.Symbol);
    }

    private void CheckTokenAndCollectionExists(string symbol)
    {
        var symbols = symbol.Split(TokenContractConstants.NFTSymbolSeparator);
        var tokenSymbol = symbols.First();
        CheckTokenExists(tokenSymbol);
        var collectionSymbol = symbols.First() + TokenContractConstants.NFTSymbolSeparator +
                               TokenContractConstants.CollectionSymbolSuffix;
        CheckTokenExists(collectionSymbol);
    }

    private void CheckTokenExists(string symbol)
    {
        var empty = new TokenInfo();
        // check old token
        var existing = GetTokenInfo(symbol);
        Assert(existing == null || existing.Equals(empty), "Token already exists.");
        // check new token
        Assert(!State.InsensitiveTokenExisting[symbol.ToUpper()], "Token already exists.");
    }

    private void CheckSymbolLength(string symbol, SymbolType symbolType)
    {
        if (symbolType == SymbolType.Token)
            Assert(symbol.Length <= TokenContractConstants.SymbolMaxLength, "Invalid token symbol length");
        if (symbolType == SymbolType.Nft || symbolType == SymbolType.NftCollection)
            Assert(symbol.Length <= TokenContractConstants.NFTSymbolMaxLength, "Invalid NFT symbol length");
    }

    private void CheckCrossChainTokenContractRegistrationControllerAuthority()
    {
        if (State.CrossChainTokenContractRegistrationController.Value == null)
            State.CrossChainTokenContractRegistrationController.Value =
                GetCrossChainTokenContractRegistrationController();
        Assert(State.CrossChainTokenContractRegistrationController.Value.OwnerAddress == Context.Sender,
            "No permission.");
    }

    private void DealWithExternalInfoDuringLocking(TransferFromInput input)
    {
        var tokenInfo = GetTokenInfo(input.Symbol);
        if (tokenInfo.ExternalInfo == null) return;
        if (tokenInfo.ExternalInfo.Value.ContainsKey(TokenContractConstants.LockCallbackExternalInfoKey))
        {
            var callbackInfo =
                JsonParser.Default.Parse<CallbackInfo>(
                    tokenInfo.ExternalInfo.Value[TokenContractConstants.LockCallbackExternalInfoKey]);
            Context.SendInline(callbackInfo.ContractAddress, callbackInfo.MethodName, input);
        }

        FireExternalLogEvent(tokenInfo, input);
    }

    private void DealWithExternalInfoDuringTransfer(TransferFromInput input)
    {
        var tokenInfo = GetTokenInfo(input.Symbol);
        if (tokenInfo.ExternalInfo == null) return;
        if (tokenInfo.ExternalInfo.Value.ContainsKey(TokenContractConstants.TransferCallbackExternalInfoKey))
        {
            var callbackInfo =
                JsonParser.Default.Parse<CallbackInfo>(
                    tokenInfo.ExternalInfo.Value[TokenContractConstants.TransferCallbackExternalInfoKey]);
            Context.SendInline(callbackInfo.ContractAddress, callbackInfo.MethodName, input);
        }

        FireExternalLogEvent(tokenInfo, input);
    }

    private void DealWithExternalInfoDuringUnlock(TransferFromInput input)
    {
        var tokenInfo = GetTokenInfo(input.Symbol);
        if (tokenInfo.ExternalInfo == null) return;
        if (tokenInfo.ExternalInfo.Value.ContainsKey(TokenContractConstants.UnlockCallbackExternalInfoKey))
        {
            var callbackInfo =
                JsonParser.Default.Parse<CallbackInfo>(
                    tokenInfo.ExternalInfo.Value[TokenContractConstants.UnlockCallbackExternalInfoKey]);
            Context.SendInline(callbackInfo.ContractAddress, callbackInfo.MethodName, input);
        }

        FireExternalLogEvent(tokenInfo, input);
    }

    private void FireExternalLogEvent(TokenInfo tokenInfo, TransferFromInput input)
    {
        if (tokenInfo.ExternalInfo.Value.ContainsKey(TokenContractConstants.LogEventExternalInfoKey))
            Context.FireLogEvent(new LogEvent
            {
                Name = tokenInfo.ExternalInfo.Value[TokenContractConstants.LogEventExternalInfoKey],
                Address = Context.Self,
                NonIndexed = input.ToByteString()
            });
    }
    
    private bool IsInLockWhiteList(Address address)
    {
        return address == GetElectionContractAddress() || address == GetVoteContractAddress();
    }

    private Address GetElectionContractAddress()
    {
        if (State.ElectionContractAddress.Value == null)
        {
            State.ElectionContractAddress.Value =
                Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
        }
        
        return State.ElectionContractAddress.Value;
    }
    
    private Address GetVoteContractAddress()
    {
        if (State.VoteContractAddress.Value == null)
        {
            State.VoteContractAddress.Value =
                Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName);
        }
        
        return State.VoteContractAddress.Value;
    }

    private TokenInfo GetTokenInfo(string symbolOrAlias)
    {
        var tokenInfo = State.TokenInfos[symbolOrAlias];
        if (tokenInfo != null) return tokenInfo;
        var actualTokenSymbol = State.SymbolAliasMap[symbolOrAlias];
        if (!string.IsNullOrEmpty(actualTokenSymbol))
        {
            tokenInfo = State.TokenInfos[actualTokenSymbol];
        }

        return tokenInfo;
    }

    private void SetTokenInfo(TokenInfo tokenInfo)
    {
        var symbol = tokenInfo.Symbol;
        State.TokenInfos[symbol] = tokenInfo;
    }
}