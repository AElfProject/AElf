using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Configuration;
using AElf.Kernel.FeatureDisable.Core;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureDisable;

public class FeatureDisableService : IFeatureDisableService, ITransientDependency
{
    private readonly IConfigurationService _configurationService;
    private readonly IBlockchainService _blockchainService;

    public FeatureDisableService(IConfigurationService configurationService, IBlockchainService blockchainService)
    {
        _configurationService = configurationService;
        _blockchainService = blockchainService;
    }

    public async Task<bool> IsFeatureDisabledAsync(params string[] featureNames)
    {
        var chain = await _blockchainService.GetChainAsync();
        var activeHeightByteString = await _configurationService.GetConfigurationDataAsync(
            FeatureDisableConstants.FeatureDisableConfigurationName,
            new ChainContext
            {
                BlockHeight = chain.BestChainHeight,
                BlockHash = chain.BestChainHash
            });
        var nameList = new StringValue();
        nameList.MergeFrom(activeHeightByteString);
        return nameList.Value.Split(',').Select(n => n.Trim()).Intersect(featureNames).Any();
    }
}