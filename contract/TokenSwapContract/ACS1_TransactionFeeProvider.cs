using System.Collections.Generic;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace TokenSwapContract
{
    public partial class TokenSwapContract
    {
        #region Views

        public override MethodFees GetMethodFee(StringValue input)
        {
            if (new List<string>
            {
                nameof(SwapToken)
            }.Contains(input.Value))
            {
                return new MethodFees
                {
                    MethodName = input.Value
                };
            }

            return State.TransactionFees[input.Value];
        }

        public override AuthorityInfo GetMethodFeeController(Empty input)
        {
            RequiredMethodFeeControllerSet();
            return State.MethodFeeController.Value;
        }

        #endregion

        public override Empty SetMethodFee(MethodFees input)
        {
            foreach (var methodFee in input.Fees)
            {
                AssertValidToken(methodFee.Symbol, methodFee.BasicFee);
            }

            RequiredMethodFeeControllerSet();
            Assert(Context.Sender == State.MethodFeeController.Value.OwnerAddress, "Unauthorized to set method fee.");
            State.TransactionFees[input.MethodName] = input;
            return new Empty();
        }

        #region private methods

        private void RequiredMethodFeeControllerSet()
        {
            if (State.MethodFeeController.Value != null) return;
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            var defaultAuthority = new AuthorityInfo
            {
                OwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()),
                ContractAddress = State.ParliamentContract.Value
            };

            State.MethodFeeController.Value = defaultAuthority;
        }

        private void AssertValidToken(string symbol, long amount)
        {
            Assert(amount >= 0, "Invalid amount.");
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            var tokenInfoInput = new GetTokenInfoInput {Symbol = symbol};
            var tokenInfo = State.TokenContract.GetTokenInfo.Call(tokenInfoInput);
            Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol), $"Token is not found. {symbol}");
        }

        #endregion
    }
}