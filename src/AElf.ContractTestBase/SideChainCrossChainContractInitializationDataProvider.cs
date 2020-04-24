using AElf.CrossChain;
using AElf.CrossChain.Application;
using Volo.Abp.Threading;

namespace AElf.ContractTestBase
{
    public class SideChainCrossChainContractInitializationDataProvider : ICrossChainContractInitializationDataProvider
    {
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

        public SideChainCrossChainContractInitializationDataProvider(
            ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
        }

        public CrossChainContractInitializationData GetContractInitializationData()
        {
            var sideChainInitializationData =
                AsyncHelper.RunSync(_sideChainInitializationDataProvider.GetChainInitializationDataAsync);

            return new CrossChainContractInitializationData
            {
                ParentChainId = _sideChainInitializationDataProvider.ParentChainId,
                CreationHeightOnParentChain = sideChainInitializationData.CreationHeightOnParentChain,
                IsPrivilegePreserved = sideChainInitializationData.ChainCreatorPrivilegePreserved
            };
        }
    }
}