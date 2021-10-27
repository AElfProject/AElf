using System.Linq;
using AElf.Providers;
using AElf.TestBase;
using Shouldly;
using Xunit;

namespace AElf
{
    public sealed class ServiceContainerTests:AElfIntegratedTest<CoreWithServiceContainerFactoryOptionsAElfTestModule>
    {
        private readonly IServiceContainer<ITestProvider> _testProviders;

        public ServiceContainerTests()
        {
            _testProviders = GetRequiredService<IServiceContainer<ITestProvider>>();
        }

        [Fact]
        public void ServiceContainerTest()
        {
            var types = _testProviders.Select(p => p.GetType()).ToList();
            types[0].ShouldBe(typeof(ATestProvider));
            types[1].ShouldBe(typeof(CTestProvider));
            types[2].ShouldBe(typeof(BTestProvider));
        }
    }
}