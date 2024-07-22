using AElf.Kernel.FeatureDisable.Core;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureDisable.Tests;

public interface IMockService
{
    Task<bool> IsFeatureADisabledAsync();
    Task<bool> IsFeatureBDisabledAsync();
    Task<bool> IsFeatureCDisabledAsync();
    Task<bool> IsFeatureDDisabledAsync();
}

public class MockService : IMockService, ITransientDependency
{
    private readonly IFeatureDisableService _featureDisableService;

    public MockService(IFeatureDisableService featureDisableService)
    {
        _featureDisableService = featureDisableService;
    }

    public Task<bool> IsFeatureADisabledAsync()
    {
        return _featureDisableService.IsFeatureDisabledAsync("FeatureA");
    }

    public Task<bool> IsFeatureBDisabledAsync()
    {
        return _featureDisableService.IsFeatureDisabledAsync("FeatureB", "FeatureBAndC");

    }

    public Task<bool> IsFeatureCDisabledAsync()
    {
        return _featureDisableService.IsFeatureDisabledAsync("FeatureC", "FeatureBAndC");

    }

    public Task<bool> IsFeatureDDisabledAsync()
    {
        return _featureDisableService.IsFeatureDisabledAsync("FeatureD");
    }
}