using Acs1;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationContract
    {
        private AuthorityInfo GetDefaultConfigurationController()
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            return new AuthorityInfo
            {
                ContractAddress = State.ParliamentContract.Value,
                OwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty())
            };
        }

        private void AssertPerformedByConfigurationController()
        {
            if (State.ConfigurationController.Value == null)
            {
                var defaultConfigurationController = GetDefaultConfigurationController();
                State.ConfigurationController.Value = defaultConfigurationController;
            }

            Assert(Context.Sender == State.ConfigurationController.Value.OwnerAddress, "No permission.");
        }

        private void AssertPerformedByConfigurationControllerOrZeroContract()
        {
            if (State.ConfigurationController.Value == null)
            {
                var defaultConfigurationController = GetDefaultConfigurationController();
                State.ConfigurationController.Value = defaultConfigurationController;
            }

            Assert(
                State.ConfigurationController.Value.OwnerAddress == Context.Sender ||
                Context.GetZeroSmartContractAddress() == Context.Sender, "No permission.");
        }
    }
}