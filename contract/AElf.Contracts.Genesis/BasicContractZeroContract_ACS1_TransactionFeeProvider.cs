using Acs1;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis
{
    public partial class BasicContractZero
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

        #region private method

        private void RequiredMethodFeeControllerSet()
        {
            if (State.MethodFeeController.Value != null) return;
            RequireParliamentContractAddressSet();

            var defaultAuthority = new AuthorityInfo
            {
                OwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()),
                ContractAddress = State.ParliamentContract.Value
            };

            State.MethodFeeController.Value = defaultAuthority;
        }

        #endregion
    }
}