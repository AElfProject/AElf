using AElf.CrossChain;
using AElf.CrossChain.Application;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    public class CrossChainContractInitializationDataProvider : ICrossChainContractInitializationDataProvider
    {
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

        public CrossChainContractInitializationDataProvider(
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