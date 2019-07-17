using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Sdk
{
    public class CacheProviderTests : SmartContractTestBase
    {
        private readonly IStateProviderFactory _providerFactory;
        private ScopedStateProvider _innerProvider;
        private IStateProvider _scopedProvider;

        public CacheProviderTests()
        {
            _providerFactory = GetRequiredService<IStateProviderFactory>();
            _innerProvider = _providerFactory.CreateStateProvider() as ScopedStateProvider;
        }

        [Fact]
        public async Task TryGetValue_Test()
        {
            GenerateStateProvider();
            
            var result = await _scopedProvider.GetAsync(new StatePath
            {
                Parts = {"test1"}
            });
            result.Length.ShouldBe(4);
            result.ShouldBe(new byte[] {0, 1, 2, 3});
        }

        private void GenerateStateProvider()
        {
            _innerProvider.ContractAddress = Address.Generate();
            
            _scopedProvider = new CachedStateProvider(_innerProvider);
            var cacheProvider = _scopedProvider as CachedStateProvider;
            cacheProvider.Cache[new ScopedStatePath
            {
                Address = _innerProvider.ContractAddress,
                Path = new StatePath
                {
                    Parts = {"test1"}
                }
            }] = new byte[] {0, 1, 2, 3};
        }
    }
}