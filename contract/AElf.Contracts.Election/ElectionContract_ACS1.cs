using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract
    {
        #region Views

        public override MethodFees GetMethodFee(StringValue input)
        {
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

        public override Empty ChangeMethodFeeController(AuthorityInfo input)
        {
            RequiredMethodFeeControllerSet();
            AssertSenderAddressWith(State.MethodFeeController.Value.OwnerAddress);
            var organizationExist = CheckOrganizationExist(input);
            Assert(organizationExist, "Invalid authority input.");

            State.MethodFeeController.Value = input;
            return new Empty();
        }

        #region private methods

        private void RequiredMethodFeeControllerSet()
        {
            if (State.MethodFeeController.Value != null) return;

            var defaultAuthority = new AuthorityInfo
            {
                OwnerAddress = GetParliamentDefaultAddress(),
                ContractAddress = Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName)
            };

            State.MethodFeeController.Value = defaultAuthority;
        }

        private void AssertSenderAddressWith(Address address)
        {
            Assert(Context.Sender == address, "Unauthorized behavior.");
        }

        private bool CheckOrganizationExist(AuthorityInfo authorityInfo)
        {
            return Context.Call<BoolValue>(authorityInfo.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.ValidateOrganizationExist),
                authorityInfo.OwnerAddress).Value;
        }

        private void AssertValidToken(string symbol, long amount)
        {
            Assert(amount >= 0, "Invalid amount.");
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            Assert(State.TokenContract.IsTokenAvailableForMethodFee.Call(new StringValue {Value = symbol}).Value,
                $"Token {symbol} cannot set as method fee.");
        }

        #endregion
    }
}