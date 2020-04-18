using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public class TokenHolderContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = TokenHolderSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.TokenHolder";
    }
}