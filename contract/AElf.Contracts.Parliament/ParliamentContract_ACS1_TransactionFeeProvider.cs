using Acs1;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Parliament
{
    public partial class ParliamentContract
    {
        public override MethodFees GetMethodFee(StringValue input)
        {
            return State.TransactionFees[input.Value];
        }

        public override Empty SetMethodFee(MethodFees input)
        {
            RequiredMethodFeeControllerSet();

            Assert(Context.Sender == State.MethodFeeController.Value.OwnerAddress, "Unauthorized to set method fee.");
            State.TransactionFees[input.MethodName] = input;

            return new Empty();
        }

        public override Empty ChangeMethodFeeController(AuthorityStuff input)
        {
            RequiredMethodFeeControllerSet();
            AssertSenderAddressWith(State.MethodFeeController.Value.OwnerAddress);
            var organizationExist = CheckOrganizationExist(input);
            Assert(organizationExist, "Invalid authority input.");

            State.MethodFeeController.Value = input;
            return new Empty();
        }

        #region private methods

        private bool CheckOrganizationExist(AuthorityStuff authorityStuff)
        {
            return Context.Call<BoolValue>(authorityStuff.ContractAddress,
                nameof(ValidateOrganizationExist), authorityStuff.OwnerAddress).Value;
        }

        private void RequiredMethodFeeControllerSet()
        {
            if (State.MethodFeeController.Value != null) return;

            var defaultAuthority = new AuthorityStuff
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