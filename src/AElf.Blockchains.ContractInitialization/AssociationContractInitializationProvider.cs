using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public class AssociationContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = AssociationSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Association";
    }
}