namespace AElf.Kernel.FeatureDisable.Core;

public interface IFeatureDisableService
{
    Task<bool> IsFeatureDisabledAsync(params string[] featureNames);
}