using System.Linq;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;

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
            var balanceOfSender = State.Balances[from][symbol];
            Assert(balanceOfSender >= amount, $"Insufficient balance.");
            var balanceOfReceiver = State.Balances[to][symbol];
            State.Balances[from][symbol] = balanceOfSender.Sub(amount);
            State.Balances[to][symbol] = balanceOfReceiver.Add(amount);
            Context.FireEvent(new Transferred()
            {
                From = from,
                To = to,
                Symbol = symbol,
                Amount = amount,
                Memo = memo
            });
        }

        private Address GenerateLockAddress(Address from, Address to, Hash txId)
        {
            var bytes = Address.TakeByAddressLength(ByteArrayHelpers.Combine(from.DumpByteArray(), to.DumpByteArray(),
                txId.DumpByteArray()));
            return Address.FromBytes(bytes);
        }

        private void AssertLockAddress(string symbol, Address address)
        {
            var symbolState = State.LockWhiteLists[symbol];
            Assert(symbolState != null, "White list of this symbol not set.");
            Assert(symbolState[address], "Not in white list.");
        }
    }
}