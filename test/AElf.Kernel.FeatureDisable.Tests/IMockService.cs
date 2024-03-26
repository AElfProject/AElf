using AElf.Kernel.FeatureManager;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureDisable.Tests;

public interface IMockService
{
    bool IsFeatureADisabled();
    bool IsFeatureBDisabled();
    bool IsFeatureCDisabled();
    bool IsFeatureDDisabled();
}

public class MockService : IMockService, ITransientDependency
{
    private readonly IFeatureDisableService _featureDisableService;

    public MockService(IFeatureDisableService featureDisableService)
    {
        _featureDisableService = featureDisableService;
    }
    
    public bool IsFeatureADisabled()
    {
        return _featureDisableService.IsFeatureDisabled("FeatureA");
    }

    public bool IsFeatureBDisabled()
    {
        return _featureDisableService.IsFeatureDisabled("FeatureB", "FeatureBAndC");
    }

    public bool IsFeatureCDisabled()
    {
        return _featureDisableService.IsFeatureDisabled("FeatureC", "FeatureBAndC");
    }

    public bool IsFeatureDDisabled()
    {
        return _featureDisableService.IsFeatureDisabled("FeatureD");
    }
}