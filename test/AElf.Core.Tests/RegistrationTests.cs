using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Providers;
using Shouldly;
using Xunit;

namespace AElf
{
    public sealed class RegistrationTests : CoreAElfTestBase
    {
        private readonly IEnumerable<ITestProvider> _testProviders;
        private readonly IServiceContainer<ITestProvider> _testProviderList;

        public RegistrationTests()
        {
            _testProviders = GetRequiredService<IEnumerable<ITestProvider>>();
            _testProviderList = GetRequiredService<IServiceContainer<ITestProvider>>();
        }

        [Fact]
        public void IocRegistration_Test()
        {
            var count = _testProviders.Count();
            count.ShouldBeGreaterThan(0);
            count.ShouldBe(_testProviders.Select(provider => provider.Name).Distinct().Count());
            _testProviderList.Count().ShouldBe(count);
            ServiceProvider.GetServices<ITestProvider>(new[] {typeof(ATestProvider)}).Count().ShouldBe(1);
        }
    }
}