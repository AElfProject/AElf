using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeatureManager;

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