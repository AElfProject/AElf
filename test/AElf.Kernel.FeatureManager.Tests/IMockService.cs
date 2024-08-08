using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureManager.Tests;

public interface IMockService
{
    Task<string> GetCurrentFeatureNameAsync();
    
    Task<bool> IsFeatureADisabledAsync();
    Task<bool> IsFeatureBDisabledAsync();
    Task<bool> IsFeatureCDisabledAsync();
    Task<bool> IsFeatureDDisabledAsync();
}

public class MockService : IMockService, ITransientDependency
{
    private const string Version1 = nameof(Version1);
    private const string Version2 = nameof(Version2);
    private const string Version3 = nameof(Version3);
    private readonly IFeatureActiveService _featureActiveService;
    private readonly IFeatureDisableService _featureDisableService;

    public MockService(IFeatureActiveService featureActiveService, IFeatureDisableService featureDisableService)
    {
        _featureActiveService = featureActiveService;
        _featureDisableService = featureDisableService;
    }

    public async Task<string> GetCurrentFeatureNameAsync()
    {
        if (await _featureActiveService.IsFeatureActive(Version3)) return Version3;

        if (await _featureActiveService.IsFeatureActive(Version2)) return Version2;

        return Version1;
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