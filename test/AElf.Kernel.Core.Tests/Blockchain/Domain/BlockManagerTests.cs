using System.Collections.Generic;

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

    [Fact]
    public async Task RemoveBlocksAsync_BatchDelete_Test()
    {
        var blocks = new List<Block>();
        var hashes = new List<Hash>();
        for (var i = 0; i < 3; i++)
        {
            var block = _kernelTestHelper.GenerateBlock(i, i == 0 ? Hash.Empty : blocks[i - 1].GetHash());
            var hash = block.GetHash();
            await _blockManager.AddBlockHeaderAsync(block.Header);
            await _blockManager.AddBlockBodyAsync(hash, block.Body);
            blocks.Add(block);
            hashes.Add(hash);
        }

        foreach (var h in hashes) (await _blockManager.GetBlockAsync(h)).ShouldNotBeNull();

        await _blockManager.RemoveBlocksAsync(hashes);

        foreach (var h in hashes) (await _blockManager.GetBlockAsync(h)).ShouldBeNull();
    }

    [Fact]
    public async Task RemoveBlocksAsync_PartialDelete_Test()
    {
        var blockA = _kernelTestHelper.GenerateBlock(0, Hash.Empty);
        var blockB = _kernelTestHelper.GenerateBlock(1, blockA.GetHash());
        var blockC = _kernelTestHelper.GenerateBlock(2, blockB.GetHash());
        var hashA = blockA.GetHash();
        var hashB = blockB.GetHash();
        var hashC = blockC.GetHash();

        await _blockManager.AddBlockHeaderAsync(blockA.Header);
        await _blockManager.AddBlockBodyAsync(hashA, blockA.Body);
        await _blockManager.AddBlockHeaderAsync(blockB.Header);
        await _blockManager.AddBlockBodyAsync(hashB, blockB.Body);
        await _blockManager.AddBlockHeaderAsync(blockC.Header);
        await _blockManager.AddBlockBodyAsync(hashC, blockC.Body);

        await _blockManager.RemoveBlocksAsync(new List<Hash> { hashA, hashB });

        (await _blockManager.GetBlockAsync(hashA)).ShouldBeNull();
        (await _blockManager.GetBlockAsync(hashB)).ShouldBeNull();
        (await _blockManager.GetBlockAsync(hashC)).ShouldNotBeNull();
    }

    [Fact]
    public async Task RemoveBlocksAsync_HeaderAndBody_Both_Deleted_Test()
    {
        var block = _kernelTestHelper.GenerateBlock(0, Hash.Empty);
        var hash = block.GetHash();
        await _blockManager.AddBlockHeaderAsync(block.Header);
        await _blockManager.AddBlockBodyAsync(hash, block.Body);

        await _blockManager.RemoveBlocksAsync(new List<Hash> { hash });

        (await _blockManager.GetBlockHeaderAsync(hash)).ShouldBeNull();
        (await _blockManager.GetBlockAsync(hash)).ShouldBeNull();
    }

    [Fact]
    public async Task RemoveBlocksAsync_EmptyList_Test()
    {
        await _blockManager.RemoveBlocksAsync(new List<Hash>());
    }

    [Fact]
    public async Task RemoveBlocksAsync_NonExistent_Test()
    {
        var fakeHash = HashHelper.ComputeFrom("nonexistent");
        await _blockManager.RemoveBlocksAsync(new List<Hash> { fakeHash });
    }

    [Fact]
    public async Task RemoveBlocksAsync_Mixed_Test()
    {
        var block = _kernelTestHelper.GenerateBlock(0, Hash.Empty);
        var hash = block.GetHash();
        await _blockManager.AddBlockHeaderAsync(block.Header);
        await _blockManager.AddBlockBodyAsync(hash, block.Body);

        var fakeHash = HashHelper.ComputeFrom("nonexistent");
        await _blockManager.RemoveBlocksAsync(new List<Hash> { hash, fakeHash });

        (await _blockManager.GetBlockAsync(hash)).ShouldBeNull();
    }
}