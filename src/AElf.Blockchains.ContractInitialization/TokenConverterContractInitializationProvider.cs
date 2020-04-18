using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public class TokenConverterContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = TokenConverterSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.TokenConverter";
    }
}