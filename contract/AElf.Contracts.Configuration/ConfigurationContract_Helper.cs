using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationContract
    {
        private void ValidateContractState(ContractReferenceState state, Hash contractSystemName)
        {
            if (state.Value != null)
                return;
            state.Value = Context.GetContractAddressByName(contractSystemName);
        }

        private Address GetOwnerAddress()
        {
            if (State.Owner.Value != null)
                return State.Owner.Value;
            ValidateContractState(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);
            var organizationAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            State.Owner.Value = organizationAddress;

            return State.Owner.Value;
        }

        private void CheckOwnerAuthority()
        {
            var owner = GetOwnerAddress();
            Assert(owner.Equals(Context.Sender), "Not authorized to do this.");
        }

        private void CheckSenderIsParliamentOrZeroContract()
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            Assert(
                State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()) == Context.Sender ||
                Context.GetZeroSmartContractAddress() == Context.Sender, "No permission.");
        }

        private void CheckSenderIsCrossChainContract()
        {
            Assert(
                Context.Sender == Context.GetContractAddressByName(SmartContractConstants.CrossChainContractSystemName),
                "Only cross chain contract can call this method.");
        }
    }
}