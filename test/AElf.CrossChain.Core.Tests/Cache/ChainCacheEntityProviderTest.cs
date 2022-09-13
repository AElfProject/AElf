using AElf.CrossChain.Cache.Infrastructure;
using Xunit;

namespace AElf.CrossChain.Cache;

public class ChainCacheEntityProviderTest : CrossChainTestBase
{
    private readonly ICrossChainCacheEntityProvider _crossChainCacheEntityProvider;

    public ChainCacheEntityProviderTest()
    {
        _crossChainCacheEntityProvider = GetRequiredService<ICrossChainCacheEntityProvider>();
    }

    [Fact]
    public void TryAdd_New()
    {
        var chainId = 123;
        _crossChainCacheEntityProvider.AddChainCacheEntity(chainId, 1);
        Assert.True(_crossChainCacheEntityProvider.Size == 1);
    }

    [Fact]
    public void TryGet_Null()
    {
        var chainId = 123;

        _crossChainCacheEntityProvider.TryGetChainCacheEntity(chainId, out var actualBlockInfoCache);
        Assert.Null(actualBlockInfoCache);
    }

    [Fact]
    public void TryGet()
    {
        var chainId = 123;

        _crossChainCacheEntityProvider.AddChainCacheEntity(chainId, 1);

        _crossChainCacheEntityProvider.TryGetChainCacheEntity(chainId, out var actualBlockInfoCache);
        Assert.Equal(1, actualBlockInfoCache.TargetChainHeight());
    }

    [Fact]
    public void TryAdd_Twice_With_SameChainId_NotSameValue()
    {
        var chainId = 123;

        _crossChainCacheEntityProvider.AddChainCacheEntity(chainId, 1);
        _crossChainCacheEntityProvider.AddChainCacheEntity(chainId, 2);
//            var blockInfoCache2 = new BlockCacheEntityProvider(2);
        Assert.True(_crossChainCacheEntityProvider.Size == 1);
        _crossChainCacheEntityProvider.TryGetChainCacheEntity(chainId, out var actualBlockInfoCache);
        Assert.Equal(1, actualBlockInfoCache.TargetChainHeight());
    }
}