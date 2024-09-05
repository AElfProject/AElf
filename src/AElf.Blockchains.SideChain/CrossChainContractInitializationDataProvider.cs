using AElf.CrossChain.Application;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain;

extern alias CrossChainCore;

public class CrossChainContractInitializationDataProvider : CrossChainCore::AElf.CrossChain.ICrossChainContractInitializationDataProvider
{
    private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

    public CrossChainContractInitializationDataProvider(
        ISideChainInitializationDataProvider sideChainInitializationDataProvider)
    {
        _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
    }

    public CrossChainCore::AElf.CrossChain.CrossChainContractInitializationData GetContractInitializationData()
    {
        var sideChainInitializationData =
            AsyncHelper.RunSync(_sideChainInitializationDataProvider.GetChainInitializationDataAsync);

        return new CrossChainCore::AElf.CrossChain.CrossChainContractInitializationData
        {
            ParentChainId = _sideChainInitializationDataProvider.ParentChainId,
            CreationHeightOnParentChain = sideChainInitializationData.CreationHeightOnParentChain,
            IsPrivilegePreserved = sideChainInitializationData.ChainCreatorPrivilegePreserved
        };
    }
}