using Acs0;
using AElf.Contracts.Parliament;
using AElf.Kernel.Proposal;
using AElf.OS.Node.Application;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Blockchains.ContractInitialization
{
    public class SideChainParliamentContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = ParliamentSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Parliament";

        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

        public SideChainParliamentContractInitializationProvider(
            ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
        }

        protected override SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            var chainInitializationData = AsyncHelper.RunSync(async () =>
                await _sideChainInitializationDataProvider.GetChainInitializationDataAsync());
            
            var parliamentInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentInitializationCallList.Add(
                nameof(ParliamentContractContainer.ParliamentContractStub.Initialize),
                new Contracts.Parliament.InitializeInput
                {
                    PrivilegedProposer = chainInitializationData.Creator,
                    ProposerAuthorityRequired = chainInitializationData.ChainCreatorPrivilegePreserved
                });
            return parliamentInitializationCallList;
        }
    }
}