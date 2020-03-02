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

        private Address GetControllerForManageConfiguration()
        {
            if (State.ConfigurationController.Value != null)
                return State.ConfigurationController.Value;
            ValidateContractState(State.ParliamentContract, SmartContractConstants.ParliamentContractSystemName);
            var organizationAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            State.ConfigurationController.Value = organizationAddress;

            return State.ConfigurationController.Value;
        }

        private void CheckControllerAuthority()
        {
            var controller = GetControllerForManageConfiguration();
            Assert(controller.Equals(Context.Sender), "Not authorized to do this.");
        }

        private void CheckSenderIsControllerOrZeroContract()
        {
            var controller = GetControllerForManageConfiguration();
            Assert(
                controller == Context.Sender ||
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