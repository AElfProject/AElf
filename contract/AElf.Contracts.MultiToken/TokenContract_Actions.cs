using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract : TokenContractImplContainer.TokenContractImplBase
{
    public override Empty InitializeFromParentChain(InitializeFromParentChainInput input)
    {
        Assert(!State.InitializedFromParentChain.Value, "MultiToken has been initialized");
        State.InitializedFromParentChain.Value = true;
        Assert(input.Creator != null, "creator should not be null");
        foreach (var pair in input.ResourceAmount) State.ResourceAmount[pair.Key] = pair.Value;

        foreach (var pair in input.RegisteredOtherTokenContractAddresses)
            State.CrossChainTransferWhiteList[pair.Key] = pair.Value;

        SetSideChainCreator(input.Creator);
        return new Empty();
    }

    /// <summary>
    ///     Register the TokenInfo into TokenContract add initial TokenContractState.LockWhiteLists;
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty Create(CreateInput input)
    {
        // can not call create on side chain
        Assert(State.SideChainCreator.Value == null, "Failed to create token if side chain creator already set.");
        var inputSymbolType = GetCreateInputSymbolType(input.Symbol);
        ChargeCreateFees();
        return inputSymbolType switch
        {
            SymbolType.NFTCollection => CreateNFTCollection(input),
            SymbolType.NFT => CreateNFTInfo(input),
            _ => CreateToken(input)
        };
    }

    private Empty CreateToken(CreateInput input, SymbolType symbolType = SymbolType.TOKEN)
    {
        AssertValidCreateInput(input, symbolType);
        var tokenInfo = new TokenInfo
        {
            Symbol = input.Symbol,
            TokenName = input.TokenName,
            TotalSupply = input.TotalSupply,
            Decimals = input.Decimals,
            Issuer = input.Issuer,
            IsBurnable = input.IsBurnable,
            IssueChainId = input.IssueChainId == 0 ? Context.ChainId : input.IssueChainId,
            ExternalInfo = input.ExternalInfo ?? new ExternalInfo()
        };
        RegisterTokenInfo(tokenInfo);
        if (string.IsNullOrEmpty(State.NativeTokenSymbol.Value))
        {
            Assert(Context.Variables.NativeSymbol == input.Symbol, "Invalid native token input.");
            State.NativeTokenSymbol.Value = input.Symbol;
        }

        var systemContractAddresses = Context.GetSystemContractNameToAddressMapping().Select(m => m.Value);
        var isSystemContractAddress = input.LockWhiteList.All(l => systemContractAddresses.Contains(l));
        Assert(isSystemContractAddress, "Addresses in lock white list should be system contract addresses");
        foreach (var address in input.LockWhiteList) State.LockWhiteLists[input.Symbol][address] = true;

        Context.LogDebug(() => $"Token created: {input.Symbol}");

        Context.Fire(new TokenCreated
        {
            Symbol = tokenInfo.Symbol,
            TokenName = tokenInfo.TokenName,
            TotalSupply = tokenInfo.TotalSupply,
            Decimals = tokenInfo.Decimals,
            Issuer = tokenInfo.Issuer,
            IsBurnable = tokenInfo.IsBurnable,
            IssueChainId = tokenInfo.IssueChainId,
            ExternalInfo = tokenInfo.ExternalInfo
        });

        return new Empty();
    }

    /// <summary>
    ///     Set primary token symbol.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty SetPrimaryTokenSymbol(SetPrimaryTokenSymbolInput input)
    {
        Assert(State.ChainPrimaryTokenSymbol.Value == null, "Failed to set primary token symbol.");
        Assert(State.TokenInfos[input.Symbol] != null, "Invalid input.");

        State.ChainPrimaryTokenSymbol.Value = input.Symbol;
        Context.Fire(new ChainPrimaryTokenSymbolSet { TokenSymbol = input.Symbol });
        return new Empty();
    }

    /// <summary>
    ///     Issue the token to issuer,then issuer will occupy the amount of token the issued.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty Issue(IssueInput input)
    {
        Assert(input.To != null, "To address not filled.");
        AssertValidMemo(input.Memo);
        var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
        Assert(tokenInfo.IssueChainId == Context.ChainId, "Unable to issue token with wrong chainId.");
        Assert(tokenInfo.Issuer == Context.Sender || Context.Sender == Context.GetZeroSmartContractAddress(),
            $"Sender is not allowed to issue token {input.Symbol}.");

        tokenInfo.Issued = tokenInfo.Issued.Add(input.Amount);
        tokenInfo.Supply = tokenInfo.Supply.Add(input.Amount);

        Assert(tokenInfo.Issued <= tokenInfo.TotalSupply, "Total supply exceeded");
        State.TokenInfos[input.Symbol] = tokenInfo;
        ModifyBalance(input.To, input.Symbol, input.Amount);

        Context.Fire(new Issued
        {
            Symbol = input.Symbol,
            Amount = input.Amount,
            To = input.To,
            Memo = input.Memo
        });
        return new Empty();
    }

    public override Empty Transfer(TransferInput input)
    {
        AssertValidToken(input.Symbol, input.Amount);
        DoTransfer(Context.Sender, input.To, input.Symbol, input.Amount, input.Memo);
        DealWithExternalInfoDuringTransfer(new TransferFromInput
        {
            From = Context.Sender,
            To = input.To,
            Amount = input.Amount,
            Symbol = input.Symbol,
            Memo = input.Memo
        });
        return new Empty();
    }

    public override Empty Lock(LockInput input)
    {
        AssertSystemContractOrLockWhiteListAddress(input.Symbol);
        Assert(Context.Origin == input.Address, "Lock behaviour should be initialed by origin address.");
        var allowance = State.Allowances[input.Address][Context.Sender][input.Symbol];
        if (allowance >= input.Amount)
            State.Allowances[input.Address][Context.Sender][input.Symbol] = allowance.Sub(input.Amount);
        AssertValidToken(input.Symbol, input.Amount);
        var fromVirtualAddress = HashHelper.ComputeFrom(Context.Sender.Value.Concat(input.Address.Value)
            .Concat(input.LockId.Value).ToArray());
        var virtualAddress = Context.ConvertVirtualAddressToContractAddress(fromVirtualAddress);
        // Transfer token to virtual address.
        DoTransfer(input.Address, virtualAddress, input.Symbol, input.Amount, input.Usage);
        DealWithExternalInfoDuringLocking(new TransferFromInput
        {
            From = input.Address,
            To = virtualAddress,
            Symbol = input.Symbol,
            Amount = input.Amount,
            Memo = input.Usage
        });
        return new Empty();
    }

    public override Empty Unlock(UnlockInput input)
    {
        AssertSystemContractOrLockWhiteListAddress(input.Symbol);
        Assert(Context.Origin == input.Address, "Unlock behaviour should be initialed by origin address.");
        AssertValidToken(input.Symbol, input.Amount);
        var fromVirtualAddress = HashHelper.ComputeFrom(Context.Sender.Value.Concat(input.Address.Value)
            .Concat(input.LockId.Value).ToArray());
        Context.SendVirtualInline(fromVirtualAddress, Context.Self, nameof(Transfer), new TransferInput
        {
            To = input.Address,
            Symbol = input.Symbol,
            Amount = input.Amount,
            Memo = input.Usage
        });
        DealWithExternalInfoDuringUnlock(new TransferFromInput
        {
            From = Context.ConvertVirtualAddressToContractAddress(fromVirtualAddress),
            To = input.Address,
            Symbol = input.Symbol,
            Amount = input.Amount,
            Memo = input.Usage
        });
        return new Empty();
    }

    public override Empty TransferFrom(TransferFromInput input)
    {
        AssertValidToken(input.Symbol, input.Amount);
        DoTransferFrom(input.From, input.To, Context.Sender, input.Symbol, input.Amount, input.Memo);
        return new Empty();
    }

    public override Empty Approve(ApproveInput input)
    {
        AssertValidToken(input.Symbol, input.Amount);
        State.Allowances[Context.Sender][input.Spender][input.Symbol] = input.Amount;
        Context.Fire(new Approved
        {
            Owner = Context.Sender,
            Spender = input.Spender,
            Symbol = input.Symbol,
            Amount = input.Amount
        });
        return new Empty();
    }

    public override Empty UnApprove(UnApproveInput input)
    {
        AssertValidToken(input.Symbol, input.Amount);
        var oldAllowance = State.Allowances[Context.Sender][input.Spender][input.Symbol];
        var amountOrAll = Math.Min(input.Amount, oldAllowance);
        State.Allowances[Context.Sender][input.Spender][input.Symbol] = oldAllowance.Sub(amountOrAll);
        Context.Fire(new UnApproved
        {
            Owner = Context.Sender,
            Spender = input.Spender,
            Symbol = input.Symbol,
            Amount = amountOrAll
        });
        return new Empty();
    }

    public override Empty Burn(BurnInput input)
    {
        var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
        Assert(tokenInfo.IsBurnable, "The token is not burnable.");
        ModifyBalance(Context.Sender, input.Symbol, -input.Amount);
        tokenInfo.Supply = tokenInfo.Supply.Sub(input.Amount);

        Context.Fire(new Burned
        {
            Burner = Context.Sender,
            Symbol = input.Symbol,
            Amount = input.Amount
        });
        return new Empty();
    }

    public override Empty CheckThreshold(CheckThresholdInput input)
    {
        var meetThreshold = false;
        var meetBalanceSymbolList = new List<string>();
        foreach (var symbolToThreshold in input.SymbolToThreshold)
        {
            if (GetBalance(input.Sender, symbolToThreshold.Key) < symbolToThreshold.Value)
                continue;
            meetBalanceSymbolList.Add(symbolToThreshold.Key);
        }

        if (meetBalanceSymbolList.Count > 0)
        {
            if (input.IsCheckAllowance)
                foreach (var symbol in meetBalanceSymbolList)
                {
                    if (State.Allowances[input.Sender][Context.Sender][symbol] <
                        input.SymbolToThreshold[symbol]) continue;
                    meetThreshold = true;
                    break;
                }
            else
                meetThreshold = true;
        }

        if (input.SymbolToThreshold.Count == 0) meetThreshold = true;

        Assert(meetThreshold, "Cannot meet the calling threshold.");
        return new Empty();
    }

    /// <summary>
    ///     Transfer from Context.Origin to Context.Sender.
    ///     Used for contract developers to receive / share profits.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty TransferToContract(TransferToContractInput input)
    {
        AssertValidToken(input.Symbol, input.Amount);

        var transferFromInput = new TransferFromInput
        {
            From = Context.Origin,
            To = Context.Sender,
            Amount = input.Amount,
            Symbol = input.Symbol,
            Memo = input.Memo
        };
        // First check allowance.
        var allowance = State.Allowances[Context.Origin][Context.Sender][input.Symbol];
        if (allowance < input.Amount)
        {
            if (IsInWhiteList(new IsInWhiteListInput { Symbol = input.Symbol, Address = Context.Sender }).Value)
            {
                DoTransfer(Context.Origin, Context.Sender, input.Symbol, input.Amount, input.Memo);
                DealWithExternalInfoDuringTransfer(transferFromInput);
                return new Empty();
            }

            Assert(false,
                $"[TransferToContract]Insufficient allowance. Token: {input.Symbol}; {allowance}/{input.Amount}." +
                $"From:{Context.Origin}\tSpender & To:{Context.Sender}");
        }

        DoTransfer(Context.Origin, Context.Sender, input.Symbol, input.Amount, input.Memo);
        DealWithExternalInfoDuringTransfer(transferFromInput);
        State.Allowances[Context.Origin][Context.Sender][input.Symbol] = allowance.Sub(input.Amount);
        return new Empty();
    }

    public override Empty AdvanceResourceToken(AdvanceResourceTokenInput input)
    {
        Assert(
            Context.Variables.GetStringArray(TokenContractConstants.PayTxFeeSymbolListName)
                .Contains(input.ResourceTokenSymbol),
            "Invalid resource token symbol.");
        State.AdvancedResourceToken[input.ContractAddress][Context.Sender][input.ResourceTokenSymbol] =
            State.AdvancedResourceToken[input.ContractAddress][Context.Sender][input.ResourceTokenSymbol]
                .Add(input.Amount);
        DoTransfer(Context.Sender, input.ContractAddress, input.ResourceTokenSymbol, input.Amount);
        return new Empty();
    }

    public override Empty TakeResourceTokenBack(TakeResourceTokenBackInput input)
    {
        var advancedAmount =
            State.AdvancedResourceToken[input.ContractAddress][Context.Sender][input.ResourceTokenSymbol];
        Assert(advancedAmount >= input.Amount, "Can't take back that more.");
        DoTransfer(input.ContractAddress, Context.Sender, input.ResourceTokenSymbol, input.Amount);
        State.AdvancedResourceToken[input.ContractAddress][Context.Sender][input.ResourceTokenSymbol] =
            advancedAmount.Sub(input.Amount);
        return new Empty();
    }

    public override Empty ValidateTokenInfoExists(ValidateTokenInfoExistsInput input)
    {
        var tokenInfo = State.TokenInfos[input.Symbol];
        if (tokenInfo == null) throw new AssertionException("Token validation failed.");

        var validationResult = tokenInfo.TokenName == input.TokenName &&
                               tokenInfo.IsBurnable == input.IsBurnable && tokenInfo.Decimals == input.Decimals &&
                               tokenInfo.Issuer == input.Issuer && tokenInfo.TotalSupply == input.TotalSupply &&
                               tokenInfo.IssueChainId == input.IssueChainId;

        if (tokenInfo.ExternalInfo != null && tokenInfo.ExternalInfo.Value.Count > 0 ||
            input.ExternalInfo != null && input.ExternalInfo.Count > 0)
        {
            validationResult = validationResult && tokenInfo.ExternalInfo.Value.Count == input.ExternalInfo.Count;
            if (tokenInfo.ExternalInfo.Value.Any(keyPair =>
                    !input.ExternalInfo.ContainsKey(keyPair.Key) || input.ExternalInfo[keyPair.Key] != keyPair.Value))
                throw new AssertionException("Token validation failed.");
        }

        Assert(validationResult, "Token validation failed.");
        return new Empty();
    }

    public override Empty ChangeTokenIssuer(ChangeTokenIssuerInput input)
    {
        var tokenInfo = State.TokenInfos[input.Symbol];
        Assert(tokenInfo != null, $"invalid token symbol: {input.Symbol}");
        // ReSharper disable once PossibleNullReferenceException
        Assert(tokenInfo.Issuer == Context.Sender && tokenInfo.IssueChainId == Context.ChainId,
            "Permission denied");
        tokenInfo.Issuer = input.NewTokenIssuer;
        State.TokenInfos[input.Symbol] = tokenInfo;
        return new Empty();
    }

    public override Empty ResetExternalInfo(ResetExternalInfoInput input)
    {
        var tokenInfo = State.TokenInfos[input.Symbol];
        Assert(tokenInfo.Issuer == Context.Sender, "No permission to reset external info.");
        tokenInfo.ExternalInfo = input.ExternalInfo;
        State.TokenInfos[input.Symbol] = tokenInfo;
        Context.Fire(new ExternalInfoChanged
        {
            Symbol = input.Symbol,
            ExternalInfo = input.ExternalInfo
        });
        return new Empty();
    }

    public override Empty AddAddressToCreateTokenWhiteList(Address input)
    {
        AssertSenderAddressWith(GetDefaultParliamentController().OwnerAddress);
        State.CreateTokenWhiteListMap[input] = true;
        return new Empty();
    }

    public override Empty RemoveAddressFromCreateTokenWhiteList(Address input)
    {
        AssertSenderAddressWith(GetDefaultParliamentController().OwnerAddress);
        State.CreateTokenWhiteListMap.Remove(input);
        return new Empty();
    }

    #region Cross chain

    public override Empty CrossChainCreateToken(CrossChainCreateTokenInput input)
    {
        var tokenContractAddress = State.CrossChainTransferWhiteList[input.FromChainId];
        Assert(tokenContractAddress != null,
            $"Token contract address of chain {ChainHelper.ConvertChainIdToBase58(input.FromChainId)} not registered.");

        var originalTransaction = Transaction.Parser.ParseFrom(input.TransactionBytes);

        AssertCrossChainTransaction(originalTransaction, tokenContractAddress, nameof(ValidateTokenInfoExists));
        var originalTransactionId = originalTransaction.GetHash();
        CrossChainVerify(originalTransactionId, input.ParentChainHeight, input.FromChainId, input.MerklePath);
        var validateTokenInfoExistsInput =
            ValidateTokenInfoExistsInput.Parser.ParseFrom(originalTransaction.Params);
        AssertNftCollectionExist(validateTokenInfoExistsInput.Symbol);
        var tokenInfo = new TokenInfo
        {
            Symbol = validateTokenInfoExistsInput.Symbol,
            TokenName = validateTokenInfoExistsInput.TokenName,
            TotalSupply = validateTokenInfoExistsInput.TotalSupply,
            Decimals = validateTokenInfoExistsInput.Decimals,
            Issuer = validateTokenInfoExistsInput.Issuer,
            IsBurnable = validateTokenInfoExistsInput.IsBurnable,
            IssueChainId = validateTokenInfoExistsInput.IssueChainId,
            ExternalInfo = new ExternalInfo { Value = { validateTokenInfoExistsInput.ExternalInfo } }
        };
        RegisterTokenInfo(tokenInfo);

        Context.Fire(new TokenCreated
        {
            Symbol = validateTokenInfoExistsInput.Symbol,
            TokenName = validateTokenInfoExistsInput.TokenName,
            TotalSupply = validateTokenInfoExistsInput.TotalSupply,
            Decimals = validateTokenInfoExistsInput.Decimals,
            Issuer = validateTokenInfoExistsInput.Issuer,
            IsBurnable = validateTokenInfoExistsInput.IsBurnable,
            IssueChainId = validateTokenInfoExistsInput.IssueChainId,
            ExternalInfo = new ExternalInfo { Value = { validateTokenInfoExistsInput.ExternalInfo } }
        });

        return new Empty();
    }

    public override Empty RegisterCrossChainTokenContractAddress(RegisterCrossChainTokenContractAddressInput input)
    {
        CheckCrossChainTokenContractRegistrationControllerAuthority();

        var originalTransaction = Transaction.Parser.ParseFrom(input.TransactionBytes);
        AssertCrossChainTransaction(originalTransaction, Context.GetZeroSmartContractAddress(input.FromChainId),
            nameof(ACS0Container.ACS0ReferenceState.ValidateSystemContractAddress));

        var validAddress = ExtractTokenContractAddress(originalTransaction.Params);

        var originalTransactionId = originalTransaction.GetHash();
        CrossChainVerify(originalTransactionId, input.ParentChainHeight, input.FromChainId, input.MerklePath);

        State.CrossChainTransferWhiteList[input.FromChainId] = validAddress;

        return new Empty();
    }

    /// <summary>
    ///     Transfer token form a chain to another chain
    ///     burn the tokens at the current chain
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty CrossChainTransfer(CrossChainTransferInput input)
    {
        AssertValidToken(input.Symbol, input.Amount);
        AssertValidMemo(input.Memo);
        var issueChainId = GetIssueChainId(input.Symbol);
        Assert(issueChainId == input.IssueChainId, "Incorrect issue chain id.");
        var burnInput = new BurnInput
        {
            Amount = input.Amount,
            Symbol = input.Symbol
        };
        Burn(burnInput);
        Context.Fire(new CrossChainTransferred
        {
            From = Context.Sender,
            To = input.To,
            Symbol = input.Symbol,
            Amount = input.Amount,
            IssueChainId = input.IssueChainId,
            Memo = input.Memo,
            ToChainId = input.ToChainId
        });
        return new Empty();
    }

    /// <summary>
    ///     Receive the token from another chain
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty CrossChainReceiveToken(CrossChainReceiveTokenInput input)
    {
        var transferTransaction = Transaction.Parser.ParseFrom(input.TransferTransactionBytes);
        var transferTransactionId = transferTransaction.GetHash();

        Assert(!State.VerifiedCrossChainTransferTransaction[transferTransactionId],
            "Token already claimed.");

        var crossChainTransferInput =
            CrossChainTransferInput.Parser.ParseFrom(transferTransaction.Params.ToByteArray());
        var symbol = crossChainTransferInput.Symbol;
        var amount = crossChainTransferInput.Amount;
        var receivingAddress = crossChainTransferInput.To;
        var targetChainId = crossChainTransferInput.ToChainId;
        var transferSender = transferTransaction.From;

        var tokenInfo = AssertValidToken(symbol, amount);
        var issueChainId = GetIssueChainId(symbol);
        Assert(issueChainId == crossChainTransferInput.IssueChainId, "Incorrect issue chain id.");
        Assert(targetChainId == Context.ChainId, "Unable to claim cross chain token.");
        var registeredTokenContractAddress = State.CrossChainTransferWhiteList[input.FromChainId];
        AssertCrossChainTransaction(transferTransaction, registeredTokenContractAddress,
            nameof(CrossChainTransfer));
        Context.LogDebug(() =>
            $"symbol == {symbol}, amount == {amount}, receivingAddress == {receivingAddress}, targetChainId == {targetChainId}");

        CrossChainVerify(transferTransactionId, input.ParentChainHeight, input.FromChainId, input.MerklePath);

        State.VerifiedCrossChainTransferTransaction[transferTransactionId] = true;
        tokenInfo.Supply = tokenInfo.Supply.Add(amount);
        Assert(tokenInfo.Supply <= tokenInfo.TotalSupply, "Total supply exceeded");
        State.TokenInfos[symbol] = tokenInfo;
        ModifyBalance(receivingAddress, symbol, amount);

        Context.Fire(new CrossChainReceived
        {
            From = transferSender,
            To = receivingAddress,
            Symbol = symbol,
            Amount = amount,
            Memo = crossChainTransferInput.Memo,
            FromChainId = input.FromChainId,
            ParentChainHeight = input.ParentChainHeight,
            IssueChainId = issueChainId,
            TransferTransactionId = transferTransactionId
        });
        return new Empty();
    }

    #endregion
}