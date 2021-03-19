using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AElf.Standards.ACS0;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
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
            Assert(State.SideChainCreator.Value == null, "Failed to create token if side chain creator already set.");
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
            if (string.IsNullOrEmpty(State.NativeTokenSymbol.Value))
            {
                Assert(Context.Variables.NativeSymbol == input.Symbol, "Invalid native token input.");
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

            Context.Fire(new TokenCreated
            {
                Symbol = input.Symbol,
                TokenName = input.TokenName,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                Issuer = input.Issuer,
                IsBurnable = input.IsBurnable,
                IssueChainId = input.IssueChainId == 0 ? Context.ChainId : input.IssueChainId
            });

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
            return new Empty();
        }
    }
}