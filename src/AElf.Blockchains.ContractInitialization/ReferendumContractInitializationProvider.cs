using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public class ReferendumContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = ReferendumSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Referendum";
    }
}