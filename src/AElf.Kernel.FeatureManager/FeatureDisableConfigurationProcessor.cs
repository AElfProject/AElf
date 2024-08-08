using System.Threading.Tasks;
using AElf.Kernel.Configuration;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureManager;

public class FeatureDisableConfigurationProcessor : IConfigurationProcessor, ITransientDependency
{
    private readonly IDisabledFeatureListProvider _disabledFeatureListProvider;

    public FeatureDisableConfigurationProcessor(IDisabledFeatureListProvider disabledFeatureListProvider)
    {
        _disabledFeatureListProvider = disabledFeatureListProvider;
    }

    public string ConfigurationName => FeatureManagerConstants.FeatureDisableConfigurationName;

    public async Task ProcessConfigurationAsync(ByteString byteString, BlockIndex blockIndex)
    {
        var featureNameList = new StringValue();
        featureNameList.MergeFrom(byteString);
        await _disabledFeatureListProvider.SetDisabledFeatureListAsync(blockIndex, featureNameList.Value);
    }
}