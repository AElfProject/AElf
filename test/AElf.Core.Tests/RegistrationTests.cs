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
            //Providers count should be 3.But its real count is 6.
            _testProviders.Count().ShouldBe(6);
        }
    }
}