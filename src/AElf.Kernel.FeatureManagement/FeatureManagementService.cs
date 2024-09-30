using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Configuration;
using AElf.Kernel.FeatureManagement.Core;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureManagement;

public class FeatureManagementService : IFeatureManagementService, ITransientDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly IConfigurationService _configurationService;
    private readonly IDisabledFeatureListProvider _disabledFeatureListProvider;

    public FeatureManagementService(IConfigurationService configurationService, IBlockchainService blockchainService,
        IDisabledFeatureListProvider disabledFeatureListProvider)
    {
        _configurationService = configurationService;
        _blockchainService = blockchainService;
        _disabledFeatureListProvider = disabledFeatureListProvider;
    }

    public async Task<bool> IsFeatureActive(string featureName)
    {
        var featureConfigurationName = GetFeatureConfigurationName(featureName);
        var chain = await _blockchainService.GetChainAsync();
        var activeHeightByteString = await _configurationService.GetConfigurationDataAsync(featureConfigurationName,
            new ChainContext
            {
                BlockHeight = chain.BestChainHeight,
                BlockHash = chain.BestChainHash
            });
        var activeHeight = new Int64Value();
        activeHeight.MergeFrom(activeHeightByteString);
        if (activeHeight.Value == 0) return false;

        return chain.BestChainHeight >= activeHeight.Value;
    }

    private string GetFeatureConfigurationName(string featureName)
    {
        return $"{FeatureManagementConstants.FeatureConfigurationNamePrefix}{featureName}";
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