using System;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
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
            var tokenInfo = new TokenInfo
            {
                Symbol = input.Symbol,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IsBurnable = input.IsBurnable,
                IssueChainId = input.IssueChainId == 0 ? Context.ChainId : input.IssueChainId
            };
            RegisterTokenInfo(tokenInfo);

            Context.LogDebug(() => $"Token created: {input.Symbol}");

            // Context.Fire(new TokenCreated
            // {
            //     Symbol = input.Symbol,
            //     TokenName = input.TokenName,
            //     TotalSupply = input.TotalSupply,
            //     Decimals = input.Decimals,
            //     Issuer = input.Issuer,
            //     IsBurnable = input.IsBurnable,
            //     IssueChainId = input.IssueChainId == 0 ? Context.ChainId : input.IssueChainId
            // });

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

            tokenInfo.Issued = tokenInfo.Issued.Add(input.Amount);
            tokenInfo.Supply = tokenInfo.Supply.Add(input.Amount);
            
            Assert(tokenInfo.Issued <= tokenInfo.TotalSupply, "Total supply exceeded");
            State.TokenInfos[input.Symbol] = tokenInfo;
            ModifyBalance(input.To, input.Symbol, input.Amount);
            // Context.Fire(new Issued
            // {
            //     Symbol = input.Symbol,
            //     Amount = input.Amount,
            //     To = input.To,
            //     Memo = input.Memo
            // });
            return new Empty();
        }

        public override Empty Transfer(TransferInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);
            DoTransfer(Context.Sender, input.To, input.Symbol, input.Amount, input.Memo);
            return new Empty();
        }
        
        public override Empty TransferFrom(TransferFromInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);
            // First check allowance.
            var allowance = State.Allowances[input.From][Context.Sender][input.Symbol];
            if (allowance < input.Amount)
            {
                Assert(false,
                    $"[TransferFrom]Insufficient allowance. Token: {input.Symbol}; {allowance}/{input.Amount}.\n" +
                    $"From:{input.From}\tSpender:{Context.Sender}\tTo:{input.To}");
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
            // Context.Fire(new Approved()
            // {
            //     Owner = Context.Sender,
            //     Spender = input.Spender,
            //     Symbol = input.Symbol,
            //     Amount = input.Amount
            // });
            return new Empty();
        }

        public override Empty UnApprove(UnApproveInput input)
        {
            AssertValidToken(input.Symbol, input.Amount);
            var oldAllowance = State.Allowances[Context.Sender][input.Spender][input.Symbol];
            var amountOrAll = Math.Min(input.Amount, oldAllowance);
            State.Allowances[Context.Sender][input.Spender][input.Symbol] = oldAllowance.Sub(amountOrAll);
            // Context.Fire(new UnApproved()
            // {
            //     Owner = Context.Sender,
            //     Spender = input.Spender,
            //     Symbol = input.Symbol,
            //     Amount = amountOrAll
            // });
            return new Empty();
        }
    }
}