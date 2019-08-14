using AElf.Kernel;

namespace AElf.CrossChain
{
    public interface IIndexedCrossChainBlockDataDiscoveryService
    {
        bool TryDiscoverIndexedParentChainBlockDataAsync(IBlock block);
        bool TryDiscoverIndexedSideChainBlockDataAsync(IBlock block);
    }
}