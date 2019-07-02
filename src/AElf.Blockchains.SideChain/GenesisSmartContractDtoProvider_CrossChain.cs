using Acs0;
using Acs7;
using AElf.Contracts.CrossChain;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateCrossChainInitializationCallList(ChainInitializationData chainInitializationData)
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContractContainer.CrossChainContractStub.Initialize),
                new AElf.Contracts.CrossChain.InitializeInput
                {
                    ParentChainId = _sideChainInitializationDataProvider.ParentChainId,
                    CreationHeightOnParentChain = chainInitializationData.CreationHeightOnParentChain
                });
            return crossChainMethodCallList;
        }
    }
}