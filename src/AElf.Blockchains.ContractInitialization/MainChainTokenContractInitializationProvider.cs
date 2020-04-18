using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public class MainChainTokenContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = TokenSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.MultiToken";
    }
}