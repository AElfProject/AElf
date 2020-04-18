using Acs0;
using AElf.Contracts.CrossChain;
using AElf.CrossChain;
using AElf.OS.Node.Application;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Blockchains.ContractInitialization
{
    public class SideChainCrossChainContractInitializationProvider : ContractInitializationProviderBase
    {
        protected override Hash ContractName { get; } = CrossChainSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.CrossChain";

        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

        public SideChainCrossChainContractInitializationProvider(
            ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
        }

        protected override SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            var chainInitializationData = AsyncHelper.RunSync(async () =>
                await _sideChainInitializationDataProvider.GetChainInitializationDataAsync());

            crossChainMethodCallList.Add(nameof(CrossChainContractContainer.CrossChainContractStub.Initialize),
                new AElf.Contracts.CrossChain.InitializeInput
                {
                    ParentChainId = _sideChainInitializationDataProvider.ParentChainId,
                    CreationHeightOnParentChain = chainInitializationData.CreationHeightOnParentChain,
                    IsPrivilegePreserved = chainInitializationData.ChainCreatorPrivilegePreserved
                });
            return crossChainMethodCallList;
        }
    }
}