using System.Linq;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using AElf.Types;

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
            Assert(tokenInfo != null && tokenInfo != new TokenInfo(), "Token is not found.");
            return tokenInfo;
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

        private void RegisterTokenInfo(TokenInfo tokenInfo)
        {
            var existing = State.TokenInfos[tokenInfo.Symbol];
            Assert(existing == null || existing == new TokenInfo(), "Token already exists.");
            Assert(!string.IsNullOrEmpty(tokenInfo.Symbol) & tokenInfo.Symbol.All(IsValidSymbolChar),
                "Invalid symbol.");
            Assert(!string.IsNullOrEmpty(tokenInfo.TokenName), "Invalid token name.");
            Assert(tokenInfo.TotalSupply > 0, "Invalid total supply.");
            Assert(tokenInfo.Issuer != null, "Invalid issuer address.");
            State.TokenInfos[tokenInfo.Symbol] = tokenInfo;
        }

        private void ValidateCrossChainContractState()
        {
            if (State.CrossChainContractReferenceState.Value == null)
                State.CrossChainContractReferenceState.Value =
                    Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
        }

        private void AssertMainChainTokenContractAddress(Address address)
        {
            Assert(address == State.MainChainTokenContractAddress.Value,
                "Incorrect main chain token contract address.");
        }

        private void CrossChainVerify(VerifyTransactionInput verifyTransactionInput)
        {
            if (State.CrossChainContractReferenceState.Value == null)
                State.CrossChainContractReferenceState.Value =
                    Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName);
            var verificationResult =
                State.CrossChainContractReferenceState.VerifyTransaction.Call(verifyTransactionInput);
            Assert(verificationResult.Value, "Verification failed.");
        }
    }
}