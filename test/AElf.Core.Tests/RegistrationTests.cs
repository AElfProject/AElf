using System.Collections.Generic;
using System.Linq;
using AElf.Providers;
using Shouldly;
using Xunit;

namespace AElf
{
    public sealed class RegistrationTests : CoreAElfTestBase
    {
        private readonly IEnumerable<ITestProvider> _testProvidersEnumerableWrap;
        private readonly IServiceContainer<ITestProvider> _testProvidersServiceContainerWrap;

        public RegistrationTests()
        {
            _testProvidersEnumerableWrap = GetRequiredService<IEnumerable<ITestProvider>>();
            _testProvidersServiceContainerWrap = GetRequiredService<IServiceContainer<ITestProvider>>();
        }

        [Fact]
        public void IocRegistration_Test()
        {
            var count = _testProvidersEnumerableWrap.Count();
            count.ShouldBeGreaterThan(0);
            count.ShouldBeGreaterThan(_testProvidersEnumerableWrap.Select(provider => provider.Name).Distinct().Count());

            var testProviderList = _testProvidersServiceContainerWrap.ToList(); 
            var count2 = testProviderList.Count;
            count2.ShouldBeGreaterThan(0);
            count2.ShouldBeGreaterThan(testProviderList.Select(provider => provider.Name).Distinct().Count());
        }
    }
}