using System;
using System.Linq;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using Org.BouncyCastle.Asn1.X509;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract : CSharpSmartContract<TokenContractState>
    {
        public Nothing Create(CreateInput input)
        {
            Assert(!string.IsNullOrEmpty(input.Symbol) & input.Symbol.All(IsValidSymbolChar),
                "Invalid symbol.");
            Assert(!string.IsNullOrEmpty(input.TokenName), "Invalid token name.");
            Assert(input.TotalSupply > 0, "Invalid total supply.");
            Assert(input.Issuer != null, "Invalid issuer address.");
            var existing = State.TokenInfos[input.Symbol];
            Assert(existing == null || existing == new TokenInfo(), "Token already exists.");
            State.TokenInfos[input.Symbol] = new TokenInfo()
            {
                Symbol = input.Symbol,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IsBurnable = input.IsBurnable
            };
            if (string.IsNullOrEmpty(State.NativeTokenSymbol.Value))
            {
                // The first created token will be the native token
                State.NativeTokenSymbol.Value = input.Symbol;
            }

            foreach (var address in input.LockWhiteList)
            {
                State.LockWhiteLists[input.Symbol][address] = true;
            }
            
            return Nothing.Instance;
        }

        public Nothing Issue(IssueInput input)
        {
            var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
            Assert(tokenInfo.Issuer == Context.Sender, "Sender is not allowed to issue this token.");
            tokenInfo.Supply = tokenInfo.Supply.Add(input.Amount);
            Assert(tokenInfo.Supply <= tokenInfo.TotalSupply, "Total supply exceeded");
            State.TokenInfos[input.Symbol] = tokenInfo;
            State.Balances[input.To][input.Symbol] = input.Amount;
            return Nothing.Instance;
        }

        public Nothing Transfer(TransferInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);
            DoTransfer(Context.Sender, input.To, input.Symbol, input.Amount, input.Memo);
            return Nothing.Instance;
        }

        public Nothing Lock(LockInput input)
        {
            AssertLockAddress(input.Symbol, input.To);
            AssertValidToken(input.Symbol, input.Amount);
            var fromVirtualAddress = Hash.FromRawBytes(Context.Sender.Value.Concat(input.LockId.Value).ToArray());
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(fromVirtualAddress);
            // Transfer token to virtual address.
            DoTransfer(input.From, virtualAddress, input.Symbol, input.Amount, input.Usage);
            return Nothing.Instance;
        }

        public Nothing Unlock(UnlockInput input)
        {
            AssertLockAddress(input.Symbol, input.To);
            AssertValidToken(input.Symbol, input.Amount);
            var fromVirtualAddress = Hash.FromRawBytes(Context.Sender.Value.Concat(input.LockId.Value).ToArray());
            Context.SendVirtualInline(fromVirtualAddress, Context.Self, nameof(Transfer), new TransferInput
            {
                To = input.From,
                Symbol = input.Symbol,
                Amount = input.Amount,
                Memo = input.Usage,
            });
            return Nothing.Instance;
        }

        public Nothing TransferFrom(TransferFromInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);
            var allowance = State.Allowances[input.From][Context.Sender][input.Symbol];
            Assert(allowance >= input.Amount, $"Insufficient allowance.");

            DoTransfer(input.From, input.To, input.Symbol, input.Amount, input.Memo);
            State.Allowances[input.From][Context.Sender][input.Symbol] = allowance.Sub(input.Amount);
            return Nothing.Instance;
        }

        public Nothing Approve(ApproveInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);
            State.Allowances[Context.Sender][input.Spender][input.Symbol] =
                State.Allowances[Context.Sender][input.Spender][input.Symbol].Add(input.Amount);
            Context.FireEvent(new Approved()
            {
                Owner = Context.Sender,
                Spender = input.Spender,
                Symbol = input.Symbol,
                Amount = input.Amount
            });
            return Nothing.Instance;
        }

        public Nothing UnApprove(UnApproveInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);
            var oldAllowance = State.Allowances[Context.Sender][input.Spender][input.Symbol];
            var amountOrAll = Math.Min(input.Amount, oldAllowance);
            State.Allowances[Context.Sender][input.Spender][input.Symbol] = oldAllowance.Sub(amountOrAll);
            Context.FireEvent(new UnApproved()
            {
                Owner = Context.Sender,
                Spender = input.Spender,
                Symbol = input.Symbol,
                Amount = amountOrAll
            });
            return Nothing.Instance;
        }

        public Nothing Burn(BurnInput input)
        {
            var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
            Assert(tokenInfo.IsBurnable, "The token is not burnable.");
            var existingBalance = State.Balances[Context.Sender][input.Symbol];
            Assert(existingBalance >= input.Amount, "Burner doesn't own enough balance.");
            State.Balances[Context.Sender][input.Symbol] = existingBalance.Sub(input.Amount);
            tokenInfo.TotalSupply = tokenInfo.TotalSupply.Sub(input.Amount);
            Context.FireEvent(new Burned()
            {
                Burner = Context.Sender,
                Symbol = input.Symbol,
                Amount = input.Amount
            });
            return Nothing.Instance;
        }

        public Nothing ChargeTransactionFees(ChargeTransactionFeesInput input)
        {
            var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
            Assert(tokenInfo.Symbol == State.NativeTokenSymbol.Value, "The paid fee is not in native token.");
            var fromAddress = Context.Sender;
            State.Balances[fromAddress][input.Symbol] = State.Balances[fromAddress][input.Symbol].Sub(input.Amount);
            State.ChargedFees[fromAddress][input.Symbol] =
                State.ChargedFees[fromAddress][input.Symbol].Add(input.Amount);
            return Nothing.Instance;
        }

        public Nothing ClaimTransactionFees(ClaimTransactionFeesInput input)
        {
            Assert(input.Symbol == State.NativeTokenSymbol.Value, "The specified token is not the native token.");
            var feePoolAddressNotSet =
                State.FeePoolAddress.Value == null || State.FeePoolAddress.Value == new Address();
            Assert(!feePoolAddressNotSet, "Fee pool address is not set.");
            var blk = Context.GetPreviousBlock();
            var senders = blk.Body.TransactionList.Select(t => t.From).ToList();
            var feePool = State.FeePoolAddress.Value;
            foreach (var sender in senders)
            {
                var fee = State.ChargedFees[sender][input.Symbol];
                State.ChargedFees[sender][input.Symbol] = 0;
                State.Balances[feePool][input.Symbol] = State.Balances[feePool][input.Symbol].Add(fee);
            }

            return Nothing.Instance;
        }

        #region ForTests

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



        #endregion
    }
}