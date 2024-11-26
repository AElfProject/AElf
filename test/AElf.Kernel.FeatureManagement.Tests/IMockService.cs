using System.Threading.Tasks;
using AElf.Kernel.FeatureManagement.Core;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureManagement.Tests;

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
    private readonly IFeatureManagementService _featureManagementService;

    public MockService(IFeatureManagementService featureManagementService)
    {
        _featureManagementService = featureManagementService;
    }

    public async Task<string> GetCurrentFeatureNameAsync()
    {
        if (await _featureManagementService.IsFeatureActive(Version3)) return Version3;

        if (await _featureManagementService.IsFeatureActive(Version2)) return Version2;

        return Version1;
    }


    public Task<bool> IsFeatureADisabledAsync()
    {
        return _featureManagementService.IsFeatureDisabledAsync("FeatureA");
    }

    public Task<bool> IsFeatureBDisabledAsync()
    {
        return _featureManagementService.IsFeatureDisabledAsync("FeatureB", "FeatureBAndC");

    }

    public Task<bool> IsFeatureCDisabledAsync()
    {
        return _featureManagementService.IsFeatureDisabledAsync("FeatureC", "FeatureBAndC");

    }

    public Task<bool> IsFeatureDDisabledAsync()
    {
        return _featureManagementService.IsFeatureDisabledAsync("FeatureD");
    }
}