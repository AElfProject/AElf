using Acs1;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Parliament
{
    public partial class ParliamentContract
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

        #region private methods

        private bool CheckOrganizationExist(AuthorityInfo authorityInfo)
        {
            return Context.Call<BoolValue>(authorityInfo.ContractAddress,
                nameof(ValidateOrganizationExist), authorityInfo.OwnerAddress).Value;
        }

        private void RequiredMethodFeeControllerSet()
        {
            if (State.MethodFeeController.Value != null) return;

            var defaultAuthority = new AuthorityInfo
            {
                OwnerAddress = State.DefaultOrganizationAddress.Value,
                ContractAddress = Context.Self
            };

            State.MethodFeeController.Value = defaultAuthority;
        }

        private void AssertSenderAddressWith(Address address)
        {
            Assert(Context.Sender == address, "Unauthorized behavior.");
        }

        #endregion
    }
}