using AElf.Kernel;

namespace AElf.CrossChain
{
    public interface IIndexedCrossChainBlockDataDiscoveryService
    {
        bool TryDiscoverCrossChainBlockDataAsync(IBlock block);
    }
}