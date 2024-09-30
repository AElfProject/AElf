using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureManagement.Core;

public interface IFeatureManagementService
{
    Task<bool> IsFeatureActive(string featureName);
    Task<bool> IsFeatureDisabledAsync(params string[] featureNames);
}

public class DefaultFeatureManagementService : IFeatureManagementService, ITransientDependency
{
    public Task<bool> IsFeatureActive(string featureName)
    {
        return Task.FromResult(false);
    }

    public Task<bool> IsFeatureDisabledAsync(params string[] featureNames)
    {
        return Task.FromResult(false);
    }
}