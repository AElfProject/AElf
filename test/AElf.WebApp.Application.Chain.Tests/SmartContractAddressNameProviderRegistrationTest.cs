using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.WebApp.Application.Chain.Tests
{
    public sealed class SmartContractAddressNameProviderRegistrationTest : WebAppTestBase
    {
        private readonly IEnumerable<ISmartContractAddressNameProvider> _smartContractAddressNameProviders;
        
        public SmartContractAddressNameProviderRegistrationTest(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _smartContractAddressNameProviders = GetRequiredService<IEnumerable<ISmartContractAddressNameProvider>>();
        }
        
        [Fact]
        public void IocRegistrationTest()
        {
            //Providers count should be 9.But its real count is 36.
            _smartContractAddressNameProviders.Count().ShouldBe(36);
        }
    }
}