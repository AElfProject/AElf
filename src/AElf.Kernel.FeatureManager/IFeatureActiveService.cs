using System.Threading.Tasks;

namespace AElf.Kernel.FeatureManager;

public interface IFeatureActiveService
{
    Task<bool> IsFeatureActive(string featureName);
}