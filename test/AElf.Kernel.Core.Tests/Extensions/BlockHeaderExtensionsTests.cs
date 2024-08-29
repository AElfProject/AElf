using System;

namespace AElf.Kernel;

[Trait("Category", AElfBlockchainModule)]
public sealed class BlockHeaderExtensionsTests : AElfKernelTestBase
{
    private readonly KernelTestHelper _kernelTestHelper;

    public BlockHeaderExtensionsTests()
    {
        _kernelTestHelper = GetRequiredService<KernelTestHelper>();
    }

    [Fact]
    public void BlockHeader_Test()
    {
        var blockHeader = _kernelTestHelper.GenerateBlock(1, Hash.Empty).Header;

        blockHeader.GetDisambiguatingHash().ShouldBe(blockHeader.GetHash());

        blockHeader.MerkleTreeRootOfTransactions = null;
        Assert.Throws<InvalidOperationException>(blockHeader.GetDisambiguatingHash);
    }
}