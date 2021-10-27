using Volo.Abp.DependencyInjection;

namespace AElf.Providers
{
    public class ATestProvider : ITestProvider, ISingletonDependency
    {
        public string Name => nameof(ATestProvider);
    }
}