using Volo.Abp.DependencyInjection;

namespace AElf.Providers
{
    public class CTestProvider : ITestProvider, ISingletonDependency
    {
        public string Name => nameof(CTestProvider);
    }
}