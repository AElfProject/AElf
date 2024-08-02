using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeatureDisable.Core;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureDisable;

public class FeatureDisableService : IFeatureDisableService, ITransientDependency
{
    private readonly IDisabledFeatureListProvider _disabledFeatureListProvider;
    private readonly IBlockchainService _blockchainService;

    public FeatureDisableService(IDisabledFeatureListProvider disabledFeatureListProvider,
        IBlockchainService blockchainService)
    {
        _disabledFeatureListProvider = disabledFeatureListProvider;
        _blockchainService = blockchainService;
    }

    public async Task<bool> IsFeatureDisabledAsync(params string[] featureNames)
    {
        var chain = await _blockchainService.GetChainAsync();
        if (chain == null || chain.BestChainHeight <= 1)
        {
            // Which means chain hasn't been created yet or only genesis block exists.
            return false;
        }

        var nameList = await _disabledFeatureListProvider.GetDisabledFeatureListAsync(new BlockIndex
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        });
        if (nameList.Length == 0)
        {
            return false;
        }

        var isDisabled = nameList.Select(n => n.Trim()).Intersect(featureNames).Any();
        return isDisabled;
    }
}