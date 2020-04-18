using AElf.Blockchains.BasicBaseChain.ContractNames;
using AElf.Types;

namespace AElf.Blockchains.ContractInitialization
{
    public class VoteContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = VoteSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Vote";
    }
}