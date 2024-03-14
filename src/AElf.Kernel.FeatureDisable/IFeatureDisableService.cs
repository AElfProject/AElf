using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureManager;

public interface IFeatureDisableService
{
    bool IsFeatureDisabled(params string[] featureNames);
}

public class FeatureDisableService : IFeatureDisableService, ITransientDependency
{
    private readonly DisableFeatureOptions _disableFeatureOptions;

    public FeatureDisableService(IOptionsSnapshot<DisableFeatureOptions> disableFeatureOptions)
    {
        _disableFeatureOptions = disableFeatureOptions.Value;
    }

    public bool IsFeatureDisabled(params string[] featureNames)
    {
        return _disableFeatureOptions.FeatureNameList.Intersect(featureNames).Any();
    }
}