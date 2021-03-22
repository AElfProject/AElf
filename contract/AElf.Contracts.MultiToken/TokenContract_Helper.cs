using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using System.Text;
using AElf.CSharp.Core;

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
            AssertValidSymbolAndAmount(symbol, amount);
            var tokenInfo = State.TokenInfos[symbol];
            Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol), $"Token is not found. {symbol}");
            return tokenInfo;
        }
        
        private void AssertValidSymbolAndAmount(string symbol, long amount)
        {
            Assert(!string.IsNullOrEmpty(symbol) && symbol.All(IsValidSymbolChar),
                "Invalid symbol.");
            Assert(amount > 0, "Invalid amount.");
        }

        private void AssertValidMemo(string memo)
        {
            Assert(memo == null || Encoding.UTF8.GetByteCount(memo) <= TokenContractConstants.MemoMaxLength,
                "Invalid memo size.");
        }

        private void DoTransfer(Address from, Address to, string symbol, long amount, string memo = null)
        {
            Assert(from != to, "Can't do transfer to sender itself.");
            AssertValidMemo(memo);
            ModifyBalance(from, symbol, -amount);
            ModifyBalance(to, symbol, amount);
            Context.Fire(new Transferred
            {
                From = from,
                To = to,
                Symbol = symbol,
                Amount = amount,
                Memo = memo ?? string.Empty
            });
        }

        private void ModifyBalance(Address address, string symbol, long addAmount)
        {
            var before = GetBalance(address, symbol);
            if (addAmount < 0 && before < -addAmount)
            {
                Assert(false,
                    $"Insufficient balance of {symbol}. Need balance: {-addAmount}; Current balance: {before}");
            }
            var target = before.Add(addAmount);
            State.Balances[address][symbol] = target;
        }

        private long GetBalance(Address address, string symbol)
        {
            return State.Balances[address][symbol];
        }

        private void RegisterTokenInfo(TokenInfo tokenInfo)
        {
            var existing = State.TokenInfos[tokenInfo.Symbol];
            Assert(existing == null || existing.Equals(new TokenInfo()), "Token already exists.");
            Assert(!string.IsNullOrEmpty(tokenInfo.Symbol) && tokenInfo.Symbol.All(IsValidSymbolChar),
                "Invalid symbol.");
            Assert(!string.IsNullOrEmpty(tokenInfo.TokenName), $"Invalid token name. {tokenInfo.Symbol}");
            Assert(tokenInfo.TotalSupply > 0, "Invalid total supply.");
            Assert(tokenInfo.Issuer != null, "Invalid issuer address.");
            State.TokenInfos[tokenInfo.Symbol] = tokenInfo;
        }

        private void AssertValidCreateInput(CreateInput input)
        {
            var isValid = input.TokenName.Length <= TokenContractConstants.TokenNameLength
                          && input.Symbol.Length > 0
                          && input.Symbol.Length <= TokenContractConstants.SymbolMaxLength
                          && input.Decimals >= 0
                          && input.Decimals <= TokenContractConstants.MaxDecimals;
            Assert(isValid, "Invalid input.");
        }
    }
}