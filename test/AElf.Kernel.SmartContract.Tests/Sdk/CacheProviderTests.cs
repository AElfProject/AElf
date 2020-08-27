using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using QuickGraph;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract
{
    public sealed class CacheProviderTests : SmartContractTestBase
    {
        private readonly IStateProviderFactory _providerFactory;
        private ScopedStateProvider _innerProvider;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly SmartContractHelper _smartContractHelper;
        private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;

        public CacheProviderTests()
        {
            _providerFactory = GetRequiredService<IStateProviderFactory>();
            _innerProvider = _providerFactory.CreateStateProvider() as ScopedStateProvider;
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _smartContractHelper = GetRequiredService<SmartContractHelper>();
            _hostSmartContractBridgeContextService = GetRequiredService<IHostSmartContractBridgeContextService>();
        }

        [Fact]
        public async Task TryGetValue_Test()
        {
            var cachedStateProvider = await GenerateStateProviderAsync();
            
            var result = cachedStateProvider.Get(new StatePath
            {
                Parts = {"test1"}
            });
            result.Length.ShouldBe(4);
            result.ShouldBe(new byte[] {0, 1, 2, 3});

            result = cachedStateProvider.Get(new StatePath
            {
                Parts = {"test2"}
            });
            result.Length.ShouldBe(4);
            result.ShouldBe(new byte[] {1, 2, 3, 4});

            result = cachedStateProvider.Get(new StatePath
            {
                Parts = {"test3"}
            });
            result.ShouldBe(null);
        }

        private async Task<CachedStateProvider> GenerateStateProviderAsync()
        {
            var chain = await _smartContractHelper.CreateChainAsync();
            var smartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
            smartContractBridgeContext.TransactionContext = new TransactionContext
            {
                Transaction = new Transaction
                {
                    To = SampleAddress.AddressList[0]
                },
                BlockHeight = chain.BestChainHeight,
                PreviousBlockHash = chain.BestChainHash
            };
            _innerProvider.ContractAddress = SampleAddress.AddressList[0];
            _innerProvider.HostSmartContractBridgeContext = smartContractBridgeContext;
            var statePath = new ScopedStatePath
            {
                Address = _innerProvider.ContractAddress,
                Path = new StatePath
                {
                    Parts = {"test2"}
                }
            };
            await _blockStateSetManger.SetBlockStateSetAsync(new BlockStateSet
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight,
                Changes =
                {
                    {
                        statePath.ToStateKey(), ByteString.CopyFrom(1, 2, 3, 4)
                    }
                }
            });
            
            var cachedStateProvider = new CachedStateProvider(_innerProvider);
            var cacheProvider = cachedStateProvider as CachedStateProvider;
            cacheProvider.Cache[new ScopedStatePath
            {
                Address = _innerProvider.ContractAddress,
                Path = new StatePath
                {
                    Parts = {"test1"}
                }
            }] = new byte[] {0, 1, 2, 3};

            return cachedStateProvider;
        }
    }

    public sealed class NullStateCacheTests
    {
        [Fact]
        public void TryGetValue_Test()
        {
            var stateCache = new NullStateCache();
            var scopedStatePath = new ScopedStatePath
            {
                Address = SampleAddress.AddressList[0],
                Path = new StatePath()
            };
            stateCache.TryGetValue(scopedStatePath, out var value).ShouldBeFalse();
            value.ShouldBeNull();
            
            stateCache[scopedStatePath].ShouldBeNull();
            stateCache[scopedStatePath] = new byte[0];
            stateCache[scopedStatePath].ShouldBeNull();
        }
    }
}