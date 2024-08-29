namespace AElf.Kernel.SmartContract.Infrastructure;

[Trait("Category", AElfBlockchainModule)]
public sealed class BlockchainExecutedDataCacheProviderTests : AElfKernelTestBase
{
    private readonly IBlockchainExecutedDataCacheProvider<int> _blockchainExecutedDataCacheProvider;

    public BlockchainExecutedDataCacheProviderTests()
    {
        _blockchainExecutedDataCacheProvider = GetRequiredService<IBlockchainExecutedDataCacheProvider<int>>();
    }

    [Fact]
    public void BlockExecutedData_Test()
    {
        _blockchainExecutedDataCacheProvider.SetBlockExecutedData("key1", 1);
        _blockchainExecutedDataCacheProvider.TryGetBlockExecutedData("key1", out var value);
        value.ShouldBe(1);

        _blockchainExecutedDataCacheProvider.RemoveBlockExecutedData("key1");
        _blockchainExecutedDataCacheProvider.TryGetBlockExecutedData("key1", out _).ShouldBeFalse();
    }

    [Fact]
    public void ChangeHeight_Test()
    {
        _blockchainExecutedDataCacheProvider.SetChangeHeight("key1", long.MaxValue);
        _blockchainExecutedDataCacheProvider.TryGetChangeHeight("key1", out var value);
        value.ShouldBe(long.MaxValue);

        _blockchainExecutedDataCacheProvider.CleanChangeHeight(long.MaxValue);
        _blockchainExecutedDataCacheProvider.TryGetChangeHeight("key1", out _).ShouldBeFalse();
    }
}