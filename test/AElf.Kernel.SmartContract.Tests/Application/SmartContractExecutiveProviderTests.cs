using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public sealed class SmartContractExecutiveProviderTests : SmartContractTestBase
    {
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;

        public SmartContractExecutiveProviderTests()
        {
            _smartContractExecutiveProvider = GetRequiredService<ISmartContractExecutiveProvider>();
        }

        [Fact]
        public void SmartContractExecutiveProvider_Test()
        {
            var address = SampleAddress.AddressList[0];
            _smartContractExecutiveProvider.TryGetValue(address, out var executives).ShouldBeFalse();
            executives.ShouldBeNull();
            _smartContractExecutiveProvider.TryRemove(address, out executives).ShouldBeFalse();
            executives.ShouldBeNull();
            
            var executivePools = _smartContractExecutiveProvider.GetExecutivePools();
            executivePools.Count.ShouldBe(0);
            var pool = _smartContractExecutiveProvider.GetPool(address);
            pool.Count.ShouldBe(0);
            _smartContractExecutiveProvider.TryGetValue(address,out executives).ShouldBeTrue();
            executives.ShouldBe(pool);
            executivePools = _smartContractExecutiveProvider.GetExecutivePools();
            executivePools.Count.ShouldBe(1);
            
            _smartContractExecutiveProvider.TryRemove(address,out executives).ShouldBeTrue();
            executives.ShouldBe(pool);
            _smartContractExecutiveProvider.TryGetValue(address, out executives).ShouldBeFalse();
            executives.ShouldBeNull();
        }
    }
}