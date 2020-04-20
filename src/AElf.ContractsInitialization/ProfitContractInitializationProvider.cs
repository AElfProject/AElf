using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Types;

namespace AElf.ContractsInitialization
{
    public class ProfitContractInitializationProvider : ContractInitializationProviderBase
    {
        public override Hash SmartContractName => ProfitSmartContractAddressNameProvider.Name;
        public override string ContractCodeName => "AElf.Contracts.Profit";
    }
}