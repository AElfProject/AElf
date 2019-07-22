using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Approved = AElf.Contracts.MultiToken.Messages.Approved;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract : TokenContractImplContainer.TokenContractImplBase
    {
        /// <summary>
        /// Register the TokenInfo into TokenContract add set true in the TokenContractState.LockWhiteLists;
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Create(CreateInput input)
        {
            var existing = State.TokenInfos[input.Symbol];
            Assert(existing == null || existing == new TokenInfo(), "Token already exists.");
            RegisterTokenInfo(new TokenInfo
            {
                Symbol = input.Symbol,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IsBurnable = input.IsBurnable
            });

            var systemContractAddresses = Context.GetSystemContractNameToAddressMapping().Select(m => m.Value);
            var isSystemContractAddress = input.LockWhiteList.All(l => systemContractAddresses.Contains(l));
            Assert(isSystemContractAddress, "Addresses in lock white list should be system contract addresses");
            foreach (var address in input.LockWhiteList)
            {
                State.LockWhiteLists[input.Symbol][address] = true;
            }

            return new Empty();
        }

        public override Empty CreateNativeToken(CreateNativeTokenInput input)
        {
            Assert(string.IsNullOrEmpty(State.NativeTokenSymbol.Value), "Native token already created.");
            State.NativeTokenSymbol.Value = input.Symbol;
            var whiteList = new List<Address>();
            foreach (var systemContractName in input.LockWhiteSystemContractNameList)
            {
                var address = Context.GetContractAddressByName(systemContractName);
                whiteList.Add(address);
            }

            var createInput = new CreateInput
            {
                Symbol = input.Symbol,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Issuer = input.Issuer,
                Decimals = input.Decimals,
                IsBurnable = true,
                LockWhiteList = {whiteList}
            };
            return Create(createInput);
        }

        /// <summary>
        /// Issue the token of corresponding contract
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
            State.Balances[input.To][input.Symbol] = input.Amount;
            return new Empty();
        }

        /// <summary>
        /// Issue the token to the system contract
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty IssueNativeToken(IssueNativeTokenInput input)
        {
            Assert(input.ToSystemContractName != null, "To address not filled.");
            Assert(input.Symbol == State.NativeTokenSymbol.Value, "Invalid native token symbol.");
            var issueInput = new IssueInput
            {
                Symbol = input.Symbol,
                Amount = input.Amount,
                Memo = input.Memo,
                To = Context.GetContractAddressByName(input.ToSystemContractName)
            };
            return Issue(issueInput);
        }

        public override Empty Transfer(TransferInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);
            DoTransfer(Context.Sender, input.To, input.Symbol, input.Amount, input.Memo);
            return new Empty();
        }

        /// <summary>
        /// Transfer token form a chain to another chain
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
            var transferTransactionId = transferTransaction.GetHash();

            Context.LogDebug(() => $"transferTransactionId == {transferTransactionId}");
            Assert(State.VerifiedCrossChainTransferTransaction[transferTransactionId] == null,
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
            if (State.CrossChainContractReferenceState.Value == null)
                State.CrossChainContractReferenceState.Value =
                    Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = transferTransactionId,
                ParentChainHeight = input.ParentChainHeight,
                VerifiedChainId = input.FromChainId
            };
            verificationInput.Path.AddRange(input.MerklePath);
            if (State.CrossChainContractReferenceState.Value == null)
                State.CrossChainContractReferenceState.Value =
                    Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
            var verificationResult =
                State.CrossChainContractReferenceState.VerifyTransaction.Call(verificationInput);
            Assert(verificationResult.Value, "Verification failed.");

            // Create token if it doesnt exist.
            var existing = State.TokenInfos[symbol];
            if (existing == null)
                RegisterTokenInfo(crossChainTransferInput.TokenInfo);

            State.VerifiedCrossChainTransferTransaction[transferTransactionId] = input;
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
            AssertValidToken(input.Symbol, input.Amount);

            // First check allowance.
            var allowance = State.Allowances[input.From][Context.Sender][input.Symbol];
            if (allowance < input.Amount)
            {
                if (IsInWhiteList(new IsInWhiteListInput {Symbol = input.Symbol, Address = Context.Sender}).Value)
                {
                    DoTransfer(input.From, input.To, input.Symbol, input.Amount, input.Memo);
                    return new Empty();
                }

                Assert(false, "Insufficient allowance.");
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
            Assert(existingBalance >= input.Amount, "Burner doesn't own enough balance.");
            State.Balances[Context.Sender][input.Symbol] = existingBalance.Sub(input.Amount);
            tokenInfo.Supply = tokenInfo.Supply.Sub(input.Amount);
            Context.Fire(new Burned()
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

            var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
            Assert(tokenInfo.Symbol == State.NativeTokenSymbol.Value, "The paid fee is not in native token.");
            var fromAddress = Context.Sender;
            var existingBalance = State.Balances[fromAddress][input.Symbol];
            Assert(existingBalance >= input.Amount, "Insufficient balance.");
            State.Balances[fromAddress][input.Symbol] = existingBalance.Sub(input.Amount);
            State.ChargedFees[fromAddress][input.Symbol] =
                State.ChargedFees[fromAddress][input.Symbol].Add(input.Amount);
            return new Empty();
        }

        public override Empty ClaimTransactionFees(ClaimTransactionFeesInput input)
        {
            Assert(input.Symbol == State.NativeTokenSymbol.Value, "The specified token is not the native token.");
            var feePoolAddressNotSet =
                State.FeePoolAddress.Value == null || State.FeePoolAddress.Value == new Address();
            Assert(!feePoolAddressNotSet, "Fee pool address is not set.");

            var transactions = Context.GetPreviousBlockTransactions();
            var senders = transactions.Select(t => t.From).ToList();
            var feePool = State.FeePoolAddress.Value;
            foreach (var sender in senders)
            {
                var fee = State.ChargedFees[sender][input.Symbol];
                State.ChargedFees[sender][input.Symbol] = 0;
                State.Balances[feePool][input.Symbol] = State.Balances[feePool][input.Symbol].Add(fee);
            }

            return new Empty();
        }

        public override Empty SetFeePoolAddress(Hash feePoolContractSystemName)
        {
            var feePoolAddress = Context.GetContractAddressByName(feePoolContractSystemName);
            var notSet = State.FeePoolAddress.Value == null || State.FeePoolAddress.Value == new Address();
            Assert(notSet, "Fee pool address already set.");
            State.FeePoolAddress.Value = feePoolAddress;
            return new Empty();
        }

        #region ForTests

        /*
        public void Create2(string symbol, int decimals, bool isBurnable, Address issuer, string tokenName,
            long totalSupply, Address whiteAddress)
        {
            Create(new CreateInput()
            {
                Symbol = symbol,
                Decimals = decimals,
                IsBurnable = isBurnable,
                Issuer = issuer,
                TokenName = tokenName,
                TotalSupply = totalSupply,
                LockWhiteList = { whiteAddress}
            });
        }

        public void Issue2(string symbol, long amount, Address to, string memo)
        {
            Issue(new IssueInput() {Symbol = symbol, Amount = amount, To = to, Memo = memo});
        }

        public void Transfer2(string symbol, long amount, Address to, string memo)
        {
            Transfer(new TransferInput() {Symbol = symbol, Amount = amount, To = to, Memo = memo});
        }

        public void Approve2(string symbol, long amount, Address spender)
        {
            Approve(new ApproveInput() {Symbol = symbol, Amount = amount, Spender = spender});
        }

        public void UnApprove2(string symbol, long amount, Address spender)
        {
            UnApprove(new UnApproveInput() {Symbol = symbol, Amount = amount, Spender = spender});
        }


        */

        #endregion
    }
}