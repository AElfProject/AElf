using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.BlockPruning.Domain;

public class BlockPruningInfoManager : IBlockPruningInfoManager, ISingletonDependency
{
    private readonly string _key;
    private readonly IBlockchainStore<BlockPruningInfo> _store;

    public BlockPruningInfoManager(IBlockchainStore<BlockPruningInfo> store,
        IStaticChainInformationProvider chainInformationProvider)
    {
        _store = store;
        _key = chainInformationProvider.ChainId.ToStorageKey();
    }

    public async Task<long> GetLastPrunedHeightAsync()
    {
        var value = await _store.GetAsync(_key);
        return value?.LastPrunedBlockHeight ?? 0;
    }

    public async Task SetLastPrunedHeightAsync(long height)
    {
        await _store.SetAsync(_key, new BlockPruningInfo
        {
            LastPrunedBlockHeight = height
        });
    }
}