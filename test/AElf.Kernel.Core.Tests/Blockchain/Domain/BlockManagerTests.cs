namespace AElf.Kernel.Blockchain.Domain;

[Trait("Category", AElfBlockchainModule)]
public sealed class BlockManagerTests : AElfKernelTestBase
{
    private readonly IBlockManager _blockManager;
    private readonly KernelTestHelper _kernelTestHelper;

    public BlockManagerTests()
    {
        _blockManager = GetRequiredService<IBlockManager>();
        _kernelTestHelper = GetRequiredService<KernelTestHelper>();
    }

    [Fact]
    public async Task GetBlock_Header_And_Body_Test()
    {
        var block = _kernelTestHelper.GenerateBlock(0, Hash.Empty);
        var blockHash = block.GetHash();
        await _blockManager.AddBlockHeaderAsync(block.Header);
        var blockHeader = await _blockManager.GetBlockHeaderAsync(blockHash);
        Assert.Equal(blockHeader, block.Header);

        var storedBlock = await _blockManager.GetBlockAsync(blockHash);
        storedBlock.ShouldBeNull();

        await _blockManager.AddBlockBodyAsync(blockHash, block.Body);

        storedBlock = await _blockManager.GetBlockAsync(blockHash);
        Assert.Equal(storedBlock.Header, block.Header);
        Assert.Equal(storedBlock.Body, block.Body);

        (await _blockManager.HasBlockAsync(blockHash)).ShouldBeTrue();

        await _blockManager.RemoveBlockAsync(blockHash);
        await _blockManager.AddBlockBodyAsync(blockHash, block.Body);

        storedBlock = await _blockManager.GetBlockAsync(blockHash);
        storedBlock.ShouldBeNull();

        (await _blockManager.HasBlockAsync(blockHash)).ShouldBeFalse();
    }
}