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

        public RegistrationTests()
        {
            _testProviders = GetRequiredService<IEnumerable<ITestProvider>>();
        }

        [Fact]
        public void IocRegistrationTest()
        {
            var count = _testProviders.Count();
            count.ShouldBeGreaterThan(0);
            count.ShouldBe(_testProviders.Select(provider => provider.Name).Distinct().Count());
        }
    }
}