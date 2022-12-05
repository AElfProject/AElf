using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureManager.Tests;

public interface IMockService
{
    Task<string> GetCurrentFeatureNameAsync();
}

public class MockService : IMockService, ITransientDependency
{
    private const string Version1 = nameof(Version1);
    private const string Version2 = nameof(Version2);
    private const string Version3 = nameof(Version3);
    private readonly IFeatureActiveService _featureActiveService;

    public MockService(IFeatureActiveService featureActiveService)
    {
        _featureActiveService = featureActiveService;
    }

    public async Task<string> GetCurrentFeatureNameAsync()
    {
        if (await _featureActiveService.IsFeatureActive(Version3)) return Version3;

        if (await _featureActiveService.IsFeatureActive(Version2)) return Version2;

        return Version1;
    }
}