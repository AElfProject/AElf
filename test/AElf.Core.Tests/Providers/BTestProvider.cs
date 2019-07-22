using Volo.Abp.DependencyInjection;

namespace AElf.Providers
{
    public class BTestProvider : ITestProvider, ISingletonDependency
    {
        public string Name => nameof(BTestProvider);
    }
}