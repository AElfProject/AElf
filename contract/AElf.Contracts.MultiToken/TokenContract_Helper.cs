using System.Linq;
using System.Text;
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
    private static bool IsValidSymbolChar(char character)
    {
        return (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9') || character == TokenContractConstants.NFTSymbolSeparator;
    }

    private bool IsValidItemIdChar(char character)
    {
        return character >= '0' && character <= '9';
    }

    private bool IsValidCreateSymbolChar(char character)
    {
        return character >= 'A' && character <= 'Z';
    }

    private TokenInfo AssertValidToken(string symbol, long amount)
    {
        AssertValidSymbolAndAmount(symbol, amount);
        var tokenInfo = State.TokenInfos[symbol];
        Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol), $"Token is not found. {symbol}");
        return tokenInfo;
    }

    private void AssertValidSymbolAndAmount(string symbol, long amount)
    {
        Assert(!string.IsNullOrEmpty(symbol) && symbol.All(IsValidSymbolChar),
            "Invalid symbol.");
        Assert(amount > 0, "Invalid amount.");
    }

    private void AssertValidMemo(string memo)
    {
        Assert(memo == null || Encoding.UTF8.GetByteCount(memo) <= TokenContractConstants.MemoMaxLength,
            "Invalid memo size.");
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

    private void ModifyFreeFeeAllowanceAmount(MethodFeeFreeAllowances freeAllowances, string symbol, long addAmount)
    {
        var freeAllowance = GetFreeFeeAllowance(freeAllowances, symbol);
        if (freeAllowance != null)
        {
            var before = freeAllowance.Amount;
            if (addAmount < 0 && before < -addAmount)
                Assert(false,
                    $"Insufficient amount of {symbol} for free fee allowance. Need amount: {-addAmount}; Current amount: {before}");

            var target = before.Add(addAmount);
            freeAllowance.Amount = target;
        }
    }

    private long GetBalance(Address address, string symbol)
    {
        return State.Balances[address][symbol];
    }

    private MethodFeeFreeAllowance GetFreeFeeAllowance(MethodFeeFreeAllowances freeAllowances, string symbol)
    {
        return freeAllowances?.Value.FirstOrDefault(a => a.Symbol == symbol);
    }

    private long GetFreeFeeAllowanceAmount(MethodFeeFreeAllowances freeAllowances, string symbol)
    {
        var existingAllowance = 0L;
        var freeAllowance = GetFreeFeeAllowance(freeAllowances, symbol);
        if (freeAllowance != null)
        {
            existingAllowance = freeAllowance.Amount;
        }

        return existingAllowance;
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
        var existing = State.TokenInfos[tokenInfo.Symbol];
        Assert(existing == null || existing.Equals(new TokenInfo()), "Token already exists.");
        Assert(!string.IsNullOrEmpty(tokenInfo.Symbol) && tokenInfo.Symbol.All(IsValidSymbolChar),
            "Invalid symbol.");
        Assert(!string.IsNullOrEmpty(tokenInfo.TokenName), "Token name can neither be null nor empty.");
        Assert(tokenInfo.TotalSupply > 0, "Invalid total supply.");
        Assert(tokenInfo.Issuer != null, "Invalid issuer address.");
        State.TokenInfos[tokenInfo.Symbol] = tokenInfo;
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
        var tokenInfo = State.TokenInfos[symbol];
        return tokenInfo.IssueChainId;
    }

    private void AssertValidCreateInput(CreateInput input, SymbolType symbolType)
    {
        Assert(input.TokenName.Length <= TokenContractConstants.TokenNameLength
               && input.Symbol.Length > 0
               && input.Decimals >= 0
               && input.Decimals <= TokenContractConstants.MaxDecimals, "Invalid input.");
        if (symbolType == SymbolType.TOKEN)
            Assert(input.Symbol.Length <= TokenContractConstants.SymbolMaxLength, "Invalid token symbol length");
        if (symbolType == SymbolType.NFT || symbolType == SymbolType.NFTCollection)
            Assert(input.Symbol.Length <= TokenContractConstants.NFTSymbolMaxLength, "Invalid NFT symbol length");
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
        var tokenInfo = State.TokenInfos[input.Symbol];
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
        var tokenInfo = State.TokenInfos[input.Symbol];
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
        var tokenInfo = State.TokenInfos[input.Symbol];
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
}