using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Providers;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf
{
    public sealed class RegistrationTests : CoreAElfTestBase
    {
        private readonly IEnumerable<ITestProvider> _testProvidersEnumerableWrap;
        private readonly IServiceContainer<ITestProvider> _testProvidersServiceContainerWrap;
        private readonly ServiceContainerFactoryOptions<ITestProvider> _serviceContainerFactoryOptions;

        public RegistrationTests()
        {
            _testProvidersEnumerableWrap = GetRequiredService<IEnumerable<ITestProvider>>();
            _testProvidersServiceContainerWrap = GetRequiredService<IServiceContainer<ITestProvider>>();
            var serviceContainerFactoryOptionsSnapshot = GetRequiredService<IOptionsSnapshot<ServiceContainerFactoryOptions<ITestProvider>>>();
            _serviceContainerFactoryOptions = serviceContainerFactoryOptionsSnapshot.Value;
        }

        [Fact]
        public void IocRegistration_Test()
        {
            var count = _testProvidersEnumerableWrap.Count();
            
            var testProvidersServiceContainerWrapList = _testProvidersServiceContainerWrap.ToList();
            var count2 = testProvidersServiceContainerWrapList.Count;
            count.ShouldBeGreaterThanOrEqualTo(count2);
            count2.ShouldBe(_serviceContainerFactoryOptions.Types.Count);

            for (int i = 0; i < count2; i++)
            {
                testProvidersServiceContainerWrapList[i].GetType().ShouldBe(_serviceContainerFactoryOptions.Types[i]);
            }
        }
    }
}