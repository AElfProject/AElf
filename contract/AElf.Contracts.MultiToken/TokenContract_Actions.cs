using System;
using System.Collections.Generic;
using System.Linq;
using Acs8;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Treasury;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Approved = AElf.Contracts.MultiToken.Messages.Approved;

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
            var existing = State.TokenInfos[input.Symbol];
            Assert(existing == null && !string.IsNullOrEmpty(input.Symbol),
                $"Token already exists. Symbol: {input.Symbol}");
            RegisterTokenInfo(new TokenInfo
            {
                Symbol = input.Symbol,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IsBurnable = input.IsBurnable,
                IsTransferDisabled = input.IsTransferDisabled
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

        /// <summary>
        /// Issue the token to issuer,then issuer will occupy the amount of token the issued.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Issue(IssueInput input)
        {
            Assert(input.To != null, "To address not filled.");
            var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
            Assert(tokenInfo.Issuer == Context.Sender || Context.Sender == Context.GetZeroSmartContractAddress(),
                $"Sender is not allowed to issue token {input.Symbol}.");
            tokenInfo.Supply = tokenInfo.Supply.Add(input.Amount);
            Assert(tokenInfo.Supply <= tokenInfo.TotalSupply, "Total supply exceeded");
            State.TokenInfos[input.Symbol] = tokenInfo;
            State.Balances[input.To][input.Symbol] = State.Balances[input.To][input.Symbol].Add(input.Amount);
            return new Empty();
        }

        public override Empty Transfer(TransferInput input)
        {
            var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
            Assert(!tokenInfo.IsTransferDisabled, "Token can't transfer.");
            DoTransfer(Context.Sender, input.To, input.Symbol, input.Amount, input.Memo);
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
            AssertValidToken(input.TokenInfo.Symbol, input.Amount);
            var burnInput = new BurnInput
            {
                Amount = input.Amount,
                Symbol = input.TokenInfo.Symbol
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
            var transferTransactionHash = transferTransaction.GetHash();

            Context.LogDebug(() => $"transferTransactionHash == {transferTransactionHash}");
            Assert(State.VerifiedCrossChainTransferTransaction[transferTransactionHash] == null,
                "Token already claimed.");

            var crossChainTransferInput =
                CrossChainTransferInput.Parser.ParseFrom(transferTransaction.Params.ToByteArray());
            var symbol = crossChainTransferInput.TokenInfo.Symbol;
            var amount = crossChainTransferInput.Amount;
            var receivingAddress = crossChainTransferInput.To;
            var targetChainId = crossChainTransferInput.ToChainId;
            var transferSender = transferTransaction.From;
            Context.LogDebug(() =>
                $"symbol == {symbol}, amount == {amount}, receivingAddress == {receivingAddress}, targetChainId == {targetChainId}");

            Assert(transferSender.Equals(Context.Sender) && targetChainId == Context.ChainId,
                "Unable to claim cross chain token.");
            if (State.CrossChainContract.Value == null)
                State.CrossChainContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = transferTransactionHash,
                ParentChainHeight = input.ParentChainHeight,
                VerifiedChainId = input.FromChainId
            };
            verificationInput.Path.AddRange(input.MerklePath);
            if (State.CrossChainContract.Value == null)
                State.CrossChainContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
            var verificationResult =
                State.CrossChainContract.VerifyTransaction.Call(verificationInput);
            Assert(verificationResult.Value, "Verification failed.");

            // Create token if it doesnt exist.
            var existing = State.TokenInfos[symbol];
            if (existing == null)
                RegisterTokenInfo(crossChainTransferInput.TokenInfo);

            State.VerifiedCrossChainTransferTransaction[transferTransactionHash] = input;
            var balanceOfReceiver = State.Balances[receivingAddress][symbol];
            State.Balances[receivingAddress][symbol] = balanceOfReceiver.Add(amount);
            return new Empty();
        }

        public override Empty Lock(LockInput input)
        {
            AssertLockAddress(input.Symbol);
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
            var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
            Assert(tokenInfo.Issuer == input.To || !tokenInfo.IsTransferDisabled, "Token can't transfer.");

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
            var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
            Assert(!tokenInfo.IsTransferDisabled, "Token can't transfer.");
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
            Assert(existingBalance >= input.Amount, "Burner doesn't own enough balance.");
            State.Balances[Context.Sender][input.Symbol] = existingBalance.Sub(input.Amount);
            tokenInfo.Supply = tokenInfo.Supply.Sub(input.Amount);
            Context.Fire(new Burned
            {
                Burner = Context.Sender,
                Symbol = input.Symbol,
                Amount = input.Amount
            });
            return new Empty();
        }

        public override Empty ChargeTransactionFees(ChargeTransactionFeesInput input)
        {
            if (input.Equals(new ChargeTransactionFeesInput()))
            {
                return new Empty();
            }

            ChargeFirstSufficientToken(input.SymbolToAmount, out var symbol,
                out var amount, out var existingBalance);

            if (State.PreviousBlockTransactionFeeTokenSymbolList.Value == null)
            {
                State.PreviousBlockTransactionFeeTokenSymbolList.Value = new TokenSymbolList();
            }

            if (!State.PreviousBlockTransactionFeeTokenSymbolList.Value.SymbolList.Contains(symbol))
            {
                State.PreviousBlockTransactionFeeTokenSymbolList.Value.SymbolList.Add(symbol);
            }

            var fromAddress = Context.Sender;
            State.Balances[fromAddress][symbol] = existingBalance.Sub(amount);
            State.ChargedFees[fromAddress][symbol] = State.ChargedFees[fromAddress][symbol].Add(amount);
            return new Empty();
        }

        public override Empty ChargeResourceToken(ChargeResourceTokenInput input)
        {
            if (input.Equals(new ChargeResourceTokenInput()))
            {
                return new Empty();
            }

            var symbolToAmount = new Dictionary<string, long>
            {
                {"CPU", State.CpuUnitPrice.Value.Mul(input.ExecutingTime)},
                {"NET", State.NetUnitPrice.Value.Mul(input.TransactionSize)},
                {"STO", State.StoUnitPrice.Value.Mul(input.WritesCount)}
            };
            foreach (var pair in symbolToAmount)
            {
                var existingBalance = State.Balances[Context.Sender][pair.Key];
                Assert(existingBalance >= pair.Value,
                    $"Insufficient resource. {pair.Key}: {existingBalance} / {pair.Value}");
                State.Balances[Context.Sender][pair.Key] = existingBalance.Sub(pair.Value);
                State.Balances[Context.Self][pair.Key] =
                    State.Balances[Context.Self][pair.Key].Add(pair.Value);
                State.ChargedResources[pair.Key] =
                    State.ChargedResources[pair.Key].Add(pair.Value);
            }

            TryToBuyMoreResourceTokenForSender();

            return new Empty();
        }

        private void TryToBuyMoreResourceTokenForSender()
        {
            var preferences = Context.Call<ResourceTokenBuyingPreferences>(Context.Sender,
                nameof(State.ResourceConsumptionContract.GetResourceTokenBuyingPreferences), new Empty());

            if (preferences.CpuThreshold > 0)
            {
                var cpuBalance = State.Balances[Context.Sender]["CPU"];
                if (cpuBalance <= preferences.CpuThreshold)
                {
                    Context.SendInline(Context.Sender, nameof(State.ResourceConsumptionContract.BuyResourceToken),
                        new BuyResourceTokenInput
                        {
                            Symbol = "CPU",
                            Amount = preferences.CpuAmount,
                            PayLimit = preferences.PayLimit
                        });
                }
            }

            if (preferences.StoAmount > 0)
            {
                var stoBalance = State.Balances[Context.Sender]["STO"];
                if (stoBalance <= preferences.StoThreshold)
                {
                    Context.SendInline(Context.Sender, nameof(State.ResourceConsumptionContract.BuyResourceToken),
                        new BuyResourceTokenInput
                        {
                            Symbol = "STO",
                            Amount = preferences.StoAmount,
                            PayLimit = preferences.PayLimit
                        });
                }
            }

            if (preferences.NetThreshold > 0)
            {
                var netBalance = State.Balances[Context.Sender]["NET"];
                if (netBalance <= preferences.NetThreshold)
                {
                    Context.SendInline(Context.Sender, nameof(State.ResourceConsumptionContract.BuyResourceToken),
                        new BuyResourceTokenInput
                        {
                            Symbol = "NET",
                            Amount = preferences.NetAmount,
                            PayLimit = preferences.PayLimit
                        });
                }
            }
        }

        private void ChargeFirstSufficientToken(MapField<string, long> symbolToAmountMap, out string symbol,
            out long amount, out long existingBalance)
        {
            symbol = Context.Variables.NativeSymbol;
            amount = 0L;
            existingBalance = 0L;
            var fromAddress = Context.Sender;

            // Traverse available token symbols, check balance one by one
            // until there's balance of one certain token is enough to pay the fee.
            foreach (var symbolToAmount in symbolToAmountMap)
            {
                existingBalance = State.Balances[fromAddress][symbolToAmount.Key];
                symbol = symbolToAmount.Key;
                amount = symbolToAmount.Value;
                if (existingBalance >= amount)
                {
                    break;
                }
            }

            // Traversed all available tokens, can't find balance of any token enough to pay transaction fee.
            Assert(existingBalance >= amount, "Insufficient balance to pay transaction fee.");
        }

        public override Empty ClaimTransactionFees(Empty input)
        {
            if (State.TreasuryContract.Value == null)
            {
                var treasuryContractAddress =
                    Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
                if (treasuryContractAddress == null)
                {
                    // Which means Treasury Contract didn't deployed yet. Ignore this method.
                    return new Empty();
                }

                State.TreasuryContract.Value = treasuryContractAddress;
            }

            if (State.PreviousBlockTransactionFeeTokenSymbolList.Value == null ||
                !State.PreviousBlockTransactionFeeTokenSymbolList.Value.SymbolList.Any())
            {
                return new Empty();
            }

            var transactions = Context.GetPreviousBlockTransactions();
            var senders = transactions.Select(t => t.From).ToList();
            foreach (var symbol in State.PreviousBlockTransactionFeeTokenSymbolList.Value.SymbolList)
            {
                var totalFee = 0L;
                foreach (var sender in senders)
                {
                    totalFee = totalFee.Add(State.ChargedFees[sender][symbol]);
                    State.ChargedFees[sender][symbol] = 0;
                }

                State.Balances[Context.Self][symbol] = State.Balances[Context.Self][symbol].Add(totalFee);

                State.TreasuryContract.Donate.Send(new DonateInput
                {
                    Symbol = symbol,
                    Amount = totalFee
                });
            }

            State.PreviousBlockTransactionFeeTokenSymbolList.Value = new TokenSymbolList();

            return new Empty();
        }

        public override Empty DonateResourceToken(Empty input)
        {
            if (State.TreasuryContract.Value == null)
            {
                var treasuryContractAddress =
                    Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
                if (treasuryContractAddress == null)
                {
                    // Which means Treasury Contract didn't deployed yet. Ignore this method.
                    return new Empty();
                }

                State.TreasuryContract.Value = treasuryContractAddress;
            }

            foreach (var symbol in TokenContractConstants.ResourceTokenSymbols)
            {
                var amount = State.ChargedResources[symbol];
                if (amount > 0)
                {
                    State.TreasuryContract.Donate.Send(new DonateInput
                    {
                        Symbol = symbol,
                        Amount = amount
                    });

                    State.ChargedResources[symbol] = 0;
                }
            }

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

        public override Empty CheckResourceToken(Empty input)
        {
            foreach (var symbol in TokenContractConstants.ResourceTokenSymbols.Except(new List<string> {"RAM"}))
            {
                var balance = State.Balances[Context.Sender][symbol];
                Assert(balance > 0, $"Contract balance of {symbol} token is not enough.");
            }

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

            Assert(0 < input.DonationPartsPerHundred && input.DonationPartsPerHundred < 100, "Invalid donation ratio.");

            State.ProfitReceivingInfos[input.ContractAddress] = input;
            return new Empty();
        }

        public override Empty ReceiveProfits(ReceiveProfitsInput input)
        {
            var profitReceivingInformation = State.ProfitReceivingInfos[input.ContractAddress];
            Assert(profitReceivingInformation.ProfitReceiverAddress == Context.Sender,
                "Only profit Beneficiary can perform this action.");
            foreach (var symbol in input.Symbols.Except(TokenContractConstants.ResourceTokenSymbols))
            {
                var profits = State.Balances[input.ContractAddress][symbol];
                State.Balances[input.ContractAddress][symbol] = 0;
                var donates = profits.Mul(profitReceivingInformation.DonationPartsPerHundred).Div(100);
                State.Balances[Context.Self][symbol] = State.Balances[Context.Self][symbol].Add(donates);
                State.TreasuryContract.Donate.Send(new DonateInput
                {
                    Symbol = symbol,
                    Amount = donates
                });
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

        public override Empty SetResourceTokenUnitPrice(SetResourceTokenUnitPriceInput input)
        {
            if (State.ZeroContract.Value == null)
            {
                State.ZeroContract.Value = Context.GetZeroSmartContractAddress();
            }

            if (State.ParliamentAuthContract.Value == null)
            {
                State.ParliamentAuthContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            }

            var contractOwner = State.ZeroContract.GetContractAuthor.Call(Context.Self);

            Assert(
                contractOwner == Context.Sender ||
                Context.Sender == State.ParliamentAuthContract.GetGenesisOwnerAddress.Call(new Empty()) ||
                Context.Sender == Context.GetContractAddressByName(SmartContractConstants.EconomicContractSystemName),
                "No permission to set resource token unit price.");

            State.CpuUnitPrice.Value = input.CpuUnitPrice;
            State.NetUnitPrice.Value = input.NetUnitPrice;
            State.StoUnitPrice.Value = input.StoUnitPrice;
            return new Empty();
        }

        public override Empty AdvanceResourceToken(AdvanceResourceTokenInput input)
        {
            Assert(TokenContractConstants.ResourceTokenSymbols.Contains(input.ResourceTokenSymbol),
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
    }
}