using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        private static bool IsValidSymbolChar(char character)
        {
            return character >= 'A' && character <= 'Z';
        }

        private TokenInfo AssertValidToken(string symbol, long amount)
        {
            Assert(!string.IsNullOrEmpty(symbol) & symbol.All(IsValidSymbolChar),
                "Invalid symbol.");
            Assert(amount > 0, "Invalid amount.");
            var tokenInfo = State.TokenInfos[symbol];
            Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol), $"Token is not found. {symbol}");
            return tokenInfo;
        }
        
        private void AssertValidSymbolAndAmount(string symbol, long amount)
        {
            Assert(!string.IsNullOrEmpty(symbol) & symbol.All(IsValidSymbolChar),
                "Invalid symbol.");
            Assert(amount > 0, "Invalid amount.");
        }

        private void DoTransfer(Address from, Address to, string symbol, long amount, string memo)
        {
            Assert(from != to, "Can't do transfer to sender itself.");
            var balanceOfSender = State.Balances[from][symbol];
            Assert(balanceOfSender >= amount, $"Insufficient balance. {symbol}: {balanceOfSender} / {amount}");
            var balanceOfReceiver = State.Balances[to][symbol];
            State.Balances[from][symbol] = balanceOfSender.Sub(amount);
            State.Balances[to][symbol] = balanceOfReceiver.Add(amount);
            Context.Fire(new Transferred()
            {
                From = from,
                To = to,
                Symbol = symbol,
                Amount = amount,
                Memo = memo
            });
        }

        private void AssertLockAddress(string symbol)
        {
            var symbolState = State.LockWhiteLists[symbol];
            Assert(symbolState != null && symbolState[Context.Sender], "Not in white list.");
        }

        private Address ExtractTokenContractAddress(ByteString bytes)
        {
            var validateSystemContractAddressInput = ValidateSystemContractAddressInput.Parser.ParseFrom(bytes);
            var validatedAddress = validateSystemContractAddressInput.Address;
            var validatedContractHashName = validateSystemContractAddressInput.SystemContractHashName;

            Assert(validatedContractHashName == SmartContractConstants.TokenContractSystemName,
                "Address validation failed.");
            return validatedAddress;
        }

        private void AssertCrossChainTransaction(Transaction originalTransaction, Address validAddress, params string[] validMethodNames)
        {
            var validateResult = validMethodNames.Contains(originalTransaction.MethodName) 
                                 && originalTransaction.To == validAddress;
            Assert(validateResult, "Invalid transaction.");
        }

        private void RegisterTokenInfo(TokenInfo tokenInfo)
        {
            var existing = State.TokenInfos[tokenInfo.Symbol];
            Assert(existing == null || existing.Equals(new TokenInfo()), "Token already exists.");
            Assert(!string.IsNullOrEmpty(tokenInfo.Symbol) & tokenInfo.Symbol.All(IsValidSymbolChar),
                "Invalid symbol.");
            Assert(!string.IsNullOrEmpty(tokenInfo.TokenName), $"Invalid token name. {tokenInfo.Symbol}");
            Assert(tokenInfo.TotalSupply > 0, "Invalid total supply.");
            Assert(tokenInfo.Issuer != null, "Invalid issuer address.");
            State.TokenInfos[tokenInfo.Symbol] = tokenInfo;
        }

        private CrossChainContractContainer.CrossChainContractReferenceState GetValidCrossChainContractReferenceState()
        {
            if (State.CrossChainContractReferenceState.Value == null)
                State.CrossChainContractReferenceState.Value =
                    Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
            return State.CrossChainContractReferenceState;
        }
        
        private void CrossChainVerify(Hash transactionId, long parentChainHeight, int chainId, IEnumerable<Hash> merklePath)
        {
            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = transactionId,
                ParentChainHeight = parentChainHeight,
                VerifiedChainId = chainId
            };
            verificationInput.Path.AddRange(merklePath);
            var verificationResult = GetValidCrossChainContractReferenceState().VerifyTransaction.Call(verificationInput);
            Assert(verificationResult.Value, "Cross chain verification failed.");
        }
        
        private Address GetOwnerAddress()
        {
            var owner = State.Owner.Value;
            if (owner != null)
                return owner;
            var parliamentAuthContractAddress =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            owner = Context.Call<Address>(parliamentAuthContractAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractReferenceState.GetGenesisOwnerAddress),
                new Empty());
            State.Owner.Value = owner;
            return owner;
        }

        private int GetIssueChainId(string symbol)
        {
            var tokenInfo = State.TokenInfos[symbol];
            return tokenInfo.IssueChainId;
        }
    }
}