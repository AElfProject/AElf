using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public class ProfitContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = ProfitSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Profit";
        
    }
}