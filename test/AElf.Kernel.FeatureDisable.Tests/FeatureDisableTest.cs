using AElf.Kernel.FeatureManager;
using AElf.TestBase;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.Kernel.FeatureDisable.Tests;

public class FeatureDisableTest : AElfIntegratedTest<FeatureDisableTestModule>
{
    private readonly IFeatureDisableService _featureDisableService;
    private readonly IMockService _mockService;

    public FeatureDisableTest()
    {
        _featureDisableService = GetRequiredService<IFeatureDisableService>();
        _mockService = GetRequiredService<IMockService>();
    }

    [Fact]
    public void IsFeatureDisabledTest()
    {
        _mockService.IsFeatureADisabled().ShouldBeTrue();
        _mockService.IsFeatureBDisabled().ShouldBeTrue();
        _mockService.IsFeatureCDisabled().ShouldBeTrue();
        _mockService.IsFeatureDDisabled().ShouldBeFalse();
    }
}