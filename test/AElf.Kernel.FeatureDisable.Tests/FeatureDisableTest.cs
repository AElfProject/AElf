using AElf.TestBase;
using Shouldly;
using Xunit;

namespace AElf.Kernel.FeatureDisable.Tests;

public class FeatureDisableTest : AElfIntegratedTest<FeatureDisableTestModule>
{
    private readonly IMockService _mockService;

    public FeatureDisableTest()
    {
        _mockService = GetRequiredService<IMockService>();
    }

    [Fact]
    public async Task IsFeatureDisabledTest()
    {
        (await _mockService.IsFeatureADisabledAsync()).ShouldBeTrue();
        (await _mockService.IsFeatureBDisabledAsync()).ShouldBeTrue();
        (await _mockService.IsFeatureCDisabledAsync()).ShouldBeTrue();
        (await _mockService.IsFeatureDDisabledAsync()).ShouldBeFalse();
    }
}