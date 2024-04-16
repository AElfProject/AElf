using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureDisable.Core;

public interface IFeatureDisableService
{
    Task<bool> IsFeatureDisabledAsync(params string[] featureNames);
}

public class DefaultFeatureDisableService : IFeatureDisableService, ITransientDependency
{
    public Task<bool> IsFeatureDisabledAsync(params string[] featureNames)
    {
        return Task.FromResult(false);
    }
}