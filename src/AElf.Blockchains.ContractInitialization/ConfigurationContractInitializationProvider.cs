using AElf.Kernel;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public class ConfigurationContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = ConfigurationSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Configuration";
    }
}