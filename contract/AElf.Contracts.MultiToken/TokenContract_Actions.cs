using System;
using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Treasury;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract : TokenContractImplContainer.TokenContractImplBase
    {
        /// <summary>
        /// Register the TokenInfo into TokenContract add initial TokenContractState.LockWhiteLists;
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Create(CreateInput input)
        {
            AssertValidCreateInput(input);
            RegisterTokenInfo(new TokenInfo
            {
                Symbol = input.Symbol,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IsBurnable = input.IsBurnable,
                IssueChainId = input.IssueChainId == 0 ? Context.ChainId : input.IssueChainId
            });
            if (string.IsNullOrEmpty(State.NativeTokenSymbol.Value))
            {
                State.NativeTokenSymbol.Value = input.Symbol;
            }

            var systemContractAddresses = Context.GetSystemContractNameToAddressMapping().Select(m => m.Value);
            var isSystemContractAddress = input.LockWhiteList.All(l => systemContractAddresses.Contains(l));
            Assert(isSystemContractAddress, "Addresses in lock white list should be system contract addresses");
            foreach (var address in input.LockWhiteList)
            {
                State.LockWhiteLists[input.Symbol][address] = true;
            }

            Context.LogDebug(() => $"Token created: {input.Symbol}");

            return new Empty();
        }

        public override Empty RegisterNativeAndResourceTokenInfo(RegisterNativeAndResourceTokenInfoInput input)
        {
            Assert(string.IsNullOrEmpty(State.NativeTokenSymbol.Value), "Native token already registered.");
            State.NativeTokenSymbol.Value = input.NativeTokenInfo.Symbol;

            var nativeTokenInfo = new TokenInfo
            {
                Symbol = input.NativeTokenInfo.Symbol,
                TokenName = input.NativeTokenInfo.TokenName,
                TotalSupply = input.NativeTokenInfo.TotalSupply,
                Issuer = input.NativeTokenInfo.Issuer,
                Decimals = input.NativeTokenInfo.Decimals,
                IsBurnable = true,
                IssueChainId = input.NativeTokenInfo.IssueChainId
            };

            RegisterTokenInfo(nativeTokenInfo);

            Assert(input.ChainPrimaryToken.IssueChainId == Context.ChainId, "Invalid primary token info.");
            State.ChainPrimaryTokenSymbol.Value = input.ChainPrimaryToken.Symbol;
            RegisterTokenInfo(input.ChainPrimaryToken);

            if (input.ResourceTokenList?.Value != null)
            {
                foreach (var resourceTokenInfo in input.ResourceTokenList.Value)
                {
                    resourceTokenInfo.Supply = 0;
                    RegisterTokenInfo(resourceTokenInfo);
                }
            }

            Context.Fire(new ChainPrimaryTokenSymbolSet {TokenSymbol = input.ChainPrimaryToken.Symbol});

            return new Empty();
        }

        /// <summary>
        /// Issue the token to issuer,then issuer will occupy the amount of token the issued.
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
            tokenInfo.Supply = tokenInfo.Supply.Add(input.Amount);
            Assert(tokenInfo.Supply.Add(tokenInfo.Burned) <= tokenInfo.TotalSupply, "Total supply exceeded");
            State.TokenInfos[input.Symbol] = tokenInfo;
            State.Balances[input.To][input.Symbol] = State.Balances[input.To][input.Symbol].Add(input.Amount);
            return new Empty();
        }

        public override Empty Transfer(TransferInput input)
        {
            AssertValidSymbolAndAmount(input.Symbol, input.Amount);
            DoTransfer(Context.Sender, input.To, input.Symbol, input.Amount, input.Memo);
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
            ValidateTokenInfoExistsInput validateTokenInfoExistsInput =
                ValidateTokenInfoExistsInput.Parser.ParseFrom(originalTransaction.Params);

            RegisterTokenInfo(new TokenInfo
            {
                Symbol = validateTokenInfoExistsInput.Symbol,
                TokenName = validateTokenInfoExistsInput.TokenName,
                TotalSupply = validateTokenInfoExistsInput.TotalSupply,
                Decimals = validateTokenInfoExistsInput.Decimals,
                Issuer = validateTokenInfoExistsInput.Issuer,
                IsBurnable = validateTokenInfoExistsInput.IsBurnable,
                IssueChainId = validateTokenInfoExistsInput.IssueChainId
            });
            return new Empty();
        }

        public override Empty RegisterCrossChainTokenContractAddress(RegisterCrossChainTokenContractAddressInput input)
        {
            var owner = GetOwnerAddress();
            Assert(Context.Sender == owner, "No permission.");

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
        /// Transfer token form a chain to another chain
        /// burn the tokens at the current chain
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty CrossChainTransfer(CrossChainTransferInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);
            AssertValidMemo(input.Memo);
            int issueChainId = GetIssueChainId(input.Symbol);
            Assert(issueChainId == input.IssueChainId, "Incorrect issue chain id.");
            Assert(State.CrossChainTransferWhiteList[input.ToChainId] != null, "Invalid transfer target chain.");
            var burnInput = new BurnInput
            {
                Amount = input.Amount,
                Symbol = input.Symbol
            };
            Burn(burnInput);
            return new Empty();
        }

        /// <summary>
        /// Receive the token from another chain
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty CrossChainReceiveToken(CrossChainReceiveTokenInput input)
        {
            var transferTransaction = Transaction.Parser.ParseFrom(input.TransferTransactionBytes);
            var transferTransactionId = transferTransaction.GetHash();

            Assert(State.VerifiedCrossChainTransferTransaction[transferTransactionId] == null,
                "Token already claimed.");

            var crossChainTransferInput =
                CrossChainTransferInput.Parser.ParseFrom(transferTransaction.Params.ToByteArray());
            var symbol = crossChainTransferInput.Symbol;
            var amount = crossChainTransferInput.Amount;
            var receivingAddress = crossChainTransferInput.To;
            var targetChainId = crossChainTransferInput.ToChainId;
            var transferSender = transferTransaction.From;

            var tokenInfo = AssertValidToken(symbol, amount);
            int issueChainId = GetIssueChainId(symbol);
            Assert(issueChainId == crossChainTransferInput.IssueChainId, "Incorrect issue chain id.");
            Assert(transferSender.Equals(Context.Sender) && targetChainId == Context.ChainId,
                "Unable to claim cross chain token.");
            var registeredTokenContractAddress = State.CrossChainTransferWhiteList[input.FromChainId];
            AssertCrossChainTransaction(transferTransaction, registeredTokenContractAddress,
                nameof(CrossChainTransfer));
            Context.LogDebug(() =>
                $"symbol == {symbol}, amount == {amount}, receivingAddress == {receivingAddress}, targetChainId == {targetChainId}");

            CrossChainVerify(transferTransactionId, input.ParentChainHeight, input.FromChainId, input.MerklePath);

            State.VerifiedCrossChainTransferTransaction[transferTransactionId] = input;
            tokenInfo.Supply = tokenInfo.Supply.Add(amount);
            Assert(tokenInfo.Supply <= tokenInfo.TotalSupply, "Total supply exceeded");
            State.TokenInfos[symbol] = tokenInfo;
            var balanceOfReceiver = State.Balances[receivingAddress][symbol];
            State.Balances[receivingAddress][symbol] = balanceOfReceiver.Add(amount);
            return new Empty();
        }

        #endregion

        public override Empty Lock(LockInput input)
        {
            AssertLockAddress(input.Symbol);
            var allowance = State.Allowances[input.Address][Context.Sender][input.Symbol];
            if (allowance >= input.Amount)
                State.Allowances[input.Address][Context.Sender][input.Symbol] = allowance.Sub(input.Amount);
            AssertValidToken(input.Symbol, input.Amount);
            var fromVirtualAddress = Hash.FromRawBytes(Context.Sender.Value.Concat(input.Address.Value)
                .Concat(input.LockId.Value).ToArray());
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(fromVirtualAddress);
            // Transfer token to virtual address.
            DoTransfer(input.Address, virtualAddress, input.Symbol, input.Amount, input.Usage);
            return new Empty();
        }

        public override Empty Unlock(UnlockInput input)
        {
            AssertLockAddress(input.Symbol);
            AssertValidToken(input.Symbol, input.Amount);
            var fromVirtualAddress = Hash.FromRawBytes(Context.Sender.Value.Concat(input.Address.Value)
                .Concat(input.LockId.Value).ToArray());
            Context.SendVirtualInline(fromVirtualAddress, Context.Self, nameof(Transfer), new TransferInput
            {
                To = input.Address,
                Symbol = input.Symbol,
                Amount = input.Amount,
                Memo = input.Usage,
            });
            return new Empty();
        }

        public override Empty TransferFrom(TransferFromInput input)
        {
            AssertValidSymbolAndAmount(input.Symbol, input.Amount);
            // First check allowance.
            var allowance = State.Allowances[input.From][Context.Sender][input.Symbol];
            if (allowance < input.Amount)
            {
                if (IsInWhiteList(new IsInWhiteListInput {Symbol = input.Symbol, Address = Context.Sender}).Value)
                {
                    DoTransfer(input.From, input.To, input.Symbol, input.Amount, input.Memo);
                    return new Empty();
                }

                Assert(false, $"Insufficient allowance. Token: {input.Symbol}; {allowance}/{input.Amount}");
            }

            DoTransfer(input.From, input.To, input.Symbol, input.Amount, input.Memo);
            State.Allowances[input.From][Context.Sender][input.Symbol] = allowance.Sub(input.Amount);
            return new Empty();
        }

        public override Empty Approve(ApproveInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);
            State.Allowances[Context.Sender][input.Spender][input.Symbol] =
                State.Allowances[Context.Sender][input.Spender][input.Symbol].Add(input.Amount);
            Context.Fire(new Approved()
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
            Context.Fire(new UnApproved()
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
            var existingBalance = State.Balances[Context.Sender][input.Symbol];
            Assert(existingBalance >= input.Amount,
                $"Burner doesn't own enough balance. Exiting balance: {existingBalance}");
            State.Balances[Context.Sender][input.Symbol] = existingBalance.Sub(input.Amount);
            tokenInfo.Supply = tokenInfo.Supply.Sub(input.Amount);
            tokenInfo.Burned = tokenInfo.Burned.Add(input.Amount);
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
                if (State.Balances[input.Sender][symbolToThreshold.Key] < symbolToThreshold.Value)
                    continue;
                meetBalanceSymbolList.Add(symbolToThreshold.Key);
            }

            if (meetBalanceSymbolList.Count > 0)
            {
                if (input.IsCheckAllowance)
                {
                    foreach (var symbol in meetBalanceSymbolList)
                    {
                        if (State.Allowances[input.Sender][Context.Sender][symbol] <
                            input.SymbolToThreshold[symbol]) continue;
                        meetThreshold = true;
                        break;
                    }
                }
                else
                {
                    meetThreshold = true;
                }
            }

            if (input.SymbolToThreshold.Count == 0)
            {
                meetThreshold = true;
            }

            Assert(meetThreshold, "Cannot meet the calling threshold.");
            return new Empty();
        }

        public override Empty SetProfitReceivingInformation(ProfitReceivingInformation input)
        {
            if (State.ZeroContract.Value == null)
            {
                State.ZeroContract.Value = Context.GetZeroSmartContractAddress();
            }

            var contractOwner = State.ZeroContract.GetContractAuthor.Call(input.ContractAddress);
            Assert(contractOwner == Context.Sender || input.ContractAddress == Context.Sender,
                "Either contract owner or contract itself can set profit receiving information.");

            Assert(0 <= input.DonationPartsPerHundred && input.DonationPartsPerHundred <= 100,
                "Invalid donation ratio.");

            State.ProfitReceivingInfos[input.ContractAddress] = input;
            return new Empty();
        }

        public override Empty ReceiveProfits(ReceiveProfitsInput input)
        {
            var profitReceivingInformation = State.ProfitReceivingInfos[input.ContractAddress];
            Assert(profitReceivingInformation.ProfitReceiverAddress == Context.Sender,
                "Only profit receiver can perform this action.");
            foreach (var symbol in input.Symbols.Except(Context.Variables.SymbolListToPayTxFee))
            {
                var profits = State.Balances[input.ContractAddress][symbol];
                State.Balances[input.ContractAddress][symbol] = 0;
                var donates = profits.Mul(profitReceivingInformation.DonationPartsPerHundred).Div(100);
                State.Balances[Context.Self][symbol] = State.Balances[Context.Self][symbol].Add(donates);
                if (State.TreasuryContract.Value != null)
                {
                    // Main Chain.
                    State.TreasuryContract.Donate.Send(new DonateInput
                    {
                        Symbol = symbol,
                        Amount = donates
                    });
                }
                else
                {
                    // Side Chain.
                    Transfer(new TransferInput
                    {
                        To = Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName),
                        Amount = donates,
                        Symbol = symbol
                    });
                }

                State.Balances[profitReceivingInformation.ProfitReceiverAddress][symbol] =
                    State.Balances[profitReceivingInformation.ProfitReceiverAddress][symbol].Add(profits.Sub(donates));
            }

            return new Empty();
        }

        /// <summary>
        /// Transfer from Context.Origin to Context.Sender.
        /// Used for contract developers to receive / share profits.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty TransferToContract(TransferToContractInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);

            // First check allowance.
            var allowance = State.Allowances[Context.Origin][Context.Sender][input.Symbol];
            if (allowance < input.Amount)
            {
                if (IsInWhiteList(new IsInWhiteListInput {Symbol = input.Symbol, Address = Context.Sender}).Value)
                {
                    DoTransfer(Context.Origin, Context.Sender, input.Symbol, input.Amount, input.Memo);
                    return new Empty();
                }

                Assert(false, $"Insufficient allowance. Token: {input.Symbol}; {allowance}/{input.Amount}");
            }

            DoTransfer(Context.Origin, Context.Sender, input.Symbol, input.Amount, input.Memo);
            State.Allowances[Context.Origin][Context.Sender][input.Symbol] = allowance.Sub(input.Amount);
            return new Empty();
        }

        public override Empty AdvanceResourceToken(AdvanceResourceTokenInput input)
        {
            Assert(Context.Variables.SymbolListToPayTxFee.Contains(input.ResourceTokenSymbol),
                "Invalid resource token symbol.");
            State.AdvancedResourceToken[input.ContractAddress][Context.Sender][input.ResourceTokenSymbol] =
                State.AdvancedResourceToken[input.ContractAddress][Context.Sender][input.ResourceTokenSymbol]
                    .Add(input.Amount);
            State.Balances[input.ContractAddress][input.ResourceTokenSymbol] =
                State.Balances[input.ContractAddress][input.ResourceTokenSymbol].Add(input.Amount);
            State.Balances[Context.Sender][input.ResourceTokenSymbol] =
                State.Balances[Context.Sender][input.ResourceTokenSymbol].Sub(input.Amount);
            return new Empty();
        }

        public override Empty TakeResourceTokenBack(TakeResourceTokenBackInput input)
        {
            var advancedAmount =
                State.AdvancedResourceToken[input.ContractAddress][Context.Sender][input.ResourceTokenSymbol];
            Assert(advancedAmount >= input.Amount, "Can't take back that more.");
            State.Balances[input.ContractAddress][input.ResourceTokenSymbol] =
                State.Balances[input.ContractAddress][input.ResourceTokenSymbol].Sub(input.Amount);
            State.Balances[Context.Sender][input.ResourceTokenSymbol] =
                State.Balances[Context.Sender][input.ResourceTokenSymbol].Add(input.Amount);
            State.AdvancedResourceToken[input.ContractAddress][Context.Sender][input.ResourceTokenSymbol] =
                advancedAmount.Sub(input.Amount);
            return new Empty();
        }

        public override Empty ValidateTokenInfoExists(ValidateTokenInfoExistsInput input)
        {
            var tokenInfo = State.TokenInfos[input.Symbol];
            bool validationResult = tokenInfo != null && tokenInfo.TokenName == input.TokenName &&
                                    tokenInfo.IsBurnable == input.IsBurnable && tokenInfo.Decimals == input.Decimals &&
                                    tokenInfo.Issuer == input.Issuer && tokenInfo.TotalSupply == input.TotalSupply &&
                                    tokenInfo.IssueChainId == input.IssueChainId;
            Assert(validationResult, "Token validation failed.");
            return new Empty();
        }
    }
}