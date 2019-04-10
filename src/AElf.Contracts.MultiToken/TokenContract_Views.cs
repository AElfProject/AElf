using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using Address = AElf.Common.Address;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        [View]
        public override TokenInfo GetTokenInfo(GetTokenInfoInput input)
        {
            return State.TokenInfos[input.Symbol];
        }

        [View]
        public override GetBalanceOutput GetBalance(GetBalanceInput input)
        {
            return new GetBalanceOutput()
            {
                Symbol = input.Symbol,
                Owner = input.Owner,
                Balance = State.Balances[input.Owner][input.Symbol]
            };
        }

        [View]
        public override GetAllowanceOutput GetAllowance(GetAllowanceInput input)
        {
            return new GetAllowanceOutput()
            {
                Symbol = input.Symbol,
                Owner = input.Owner,
                Spender = input.Spender,
                Allowance = State.Allowances[input.Owner][input.Spender][input.Symbol]
            };
        }

        #region ForTests
        /*
        [View]
        
        public string GetTokenInfo2(string symbol)
        {
            return GetTokenInfo(new GetTokenInfoInput() {Symbol = symbol}).ToString();
        }

        [View]
        public string GetBalance2(string symbol, Address owner)
        {
            return GetBalance(
                new GetBalanceInput() {Symbol = symbol, Owner = owner})?.ToString();
        }

        [View]
        public string GetAllowance2(string symbol, Address owner, Address spender)
        {
            return GetAllowance(new GetAllowanceInput()
            {
                Owner = owner,
                Symbol = symbol,
                Spender = spender
            })?.ToString();
        }
        */
        #endregion
    }
}