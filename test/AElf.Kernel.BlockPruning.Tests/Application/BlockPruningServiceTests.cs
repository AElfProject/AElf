using System.Collections.Generic;
using AElf.Kernel.BlockPruning.Application;
using AElf.Kernel.BlockPruning.Domain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.Kernel.BlockPruning;

/// <summary>
/// Chain layout from MockChainAsync (LIB=5, BestChainHeight=11):
///   Height: 1(genesis) -> 2 -> 3 -> 4 -> 5(LIB) -> 6 -> 7 -> 8 -> 9 -> 10 -> 11
///
/// BlockPruningServiceTestModule: Enabled=true, RetainDistance=2, BatchSize=100
///   => pruneTarget = LIB - RetainDistance = 5 - 2 = 3
///   => prunable range: heights 2~3 (genesis at 1 is hard-protected)
/// </summary>
public sealed class BlockPruningServiceTests : BlockPruningServiceTestBase
{
    private readonly IBlockchainService _blockchainService;
    private readonly IBlockManager _blockManager;
    private readonly IBlockPruningInfoManager _blockPruningInfoManager;
    private readonly IBlockPruningService _blockPruningService;
    private readonly IChainManager _chainManager;

    public BlockPruningServiceTests()
    {
        _blockPruningService = GetRequiredService<IBlockPruningService>();
        _blockPruningInfoManager = GetRequiredService<IBlockPruningInfoManager>();
        _blockchainService = GetRequiredService<IBlockchainService>();
        _blockManager = GetRequiredService<IBlockManager>();
        _chainManager = GetRequiredService<IChainManager>();
    }

    [Fact]
    public async Task PruneBlockchainData_CompletePruningFlow_Test()
    {
        var chain = await _blockchainService.GetChainAsync();
        chain.LastIrreversibleBlockHeight.ShouldBe(5);

        await _blockPruningService.PruneBlockchainDataAsync();

        var lastPruned = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        lastPruned.ShouldBe(3);

        for (var h = 2L; h <= 3; h++)
        {
            var idx = await _chainManager.GetChainBlockIndexAsync(h);
            idx.ShouldNotBeNull();
            var blockHash = idx.BlockHash;

            (await _blockManager.GetBlockAsync(blockHash)).ShouldBeNull();
            (await _blockManager.GetBlockHeaderAsync(blockHash)).ShouldBeNull();
            (await _chainManager.GetChainBlockLinkAsync(blockHash)).ShouldBeNull();
        }
    }

    [Fact]
    public async Task PruneBlockchainData_ChainBlockIndex_Preserved_Test()
    {
        await _blockPruningService.PruneBlockchainDataAsync();

        for (var h = 2L; h <= 3; h++)
        {
            var idx = await _chainManager.GetChainBlockIndexAsync(h);
            idx.ShouldNotBeNull();
            idx.BlockHash.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task PruneBlockchainData_RetainedRange_Unaffected_Test()
    {
        await _blockPruningService.PruneBlockchainDataAsync();

        for (var h = 4L; h <= 5; h++)
        {
            var idx = await _chainManager.GetChainBlockIndexAsync(h);
            idx.ShouldNotBeNull();
            (await _blockManager.GetBlockAsync(idx.BlockHash)).ShouldNotBeNull();
            (await _chainManager.GetChainBlockLinkAsync(idx.BlockHash)).ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task PruneBlockchainData_GenesisBlock_Protected_Test()
    {
        await _blockPruningService.PruneBlockchainDataAsync();

        var chain = await _blockchainService.GetChainAsync();
        var genesisBlock = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);
        genesisBlock.ShouldNotBeNull();
    }

    [Fact]
    public async Task PruneBlockchainData_Idempotent_Test()
    {
        await _blockPruningService.PruneBlockchainDataAsync();
        var firstPrunedHeight = await _blockPruningInfoManager.GetLastPrunedHeightAsync();

        await _blockPruningService.PruneBlockchainDataAsync();
        var secondPrunedHeight = await _blockPruningInfoManager.GetLastPrunedHeightAsync();

        secondPrunedHeight.ShouldBe(firstPrunedHeight);
    }
}

/// <summary>
/// BlockPruningBatchTestModule: Enabled=true, RetainDistance=0, BatchSize=2
///   => pruneTarget = LIB - 0 = 5
///   => prunable: heights 2~5, BatchSize=2 => 2 batches (2~3, 4~5)
/// </summary>
public sealed class BlockPruningBatchTests : BlockPruningBatchTestBase
{
    private readonly IBlockchainService _blockchainService;
    private readonly IBlockManager _blockManager;
    private readonly IBlockPruningInfoManager _blockPruningInfoManager;
    private readonly IBlockPruningService _blockPruningService;
    private readonly IChainManager _chainManager;

    public BlockPruningBatchTests()
    {
        _blockPruningService = GetRequiredService<IBlockPruningService>();
        _blockPruningInfoManager = GetRequiredService<IBlockPruningInfoManager>();
        _blockchainService = GetRequiredService<IBlockchainService>();
        _blockManager = GetRequiredService<IBlockManager>();
        _chainManager = GetRequiredService<IChainManager>();
    }

    [Fact]
    public async Task PruneBlockchainData_MultipleBatches_Test()
    {
        await _blockPruningService.PruneBlockchainDataAsync();

        var lastPruned = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        lastPruned.ShouldBe(5);

        for (var h = 2L; h <= 5; h++)
        {
            var idx = await _chainManager.GetChainBlockIndexAsync(h);
            idx.ShouldNotBeNull();
            (await _blockManager.GetBlockAsync(idx.BlockHash)).ShouldBeNull();
        }
    }

    [Fact]
    public async Task PruneBlockchainData_GenesisProtected_WithBatch_Test()
    {
        await _blockPruningService.PruneBlockchainDataAsync();

        var genesisIdx = await _chainManager.GetChainBlockIndexAsync(1);
        genesisIdx.ShouldNotBeNull();
        (await _blockManager.GetBlockAsync(genesisIdx.BlockHash)).ShouldNotBeNull();
    }

    [Fact]
    public async Task PruneBlockchainData_HigherBlocks_Unaffected_Test()
    {
        await _blockPruningService.PruneBlockchainDataAsync();

        for (var h = 6L; h <= 11; h++)
        {
            var block = await _blockchainService.GetBlockByHeightInBestChainBranchAsync(h);
            block.ShouldNotBeNull();
        }
    }
}

/// <summary>
/// BlockPruningThresholdTestModule: Enabled=true, RetainDistance=2, PruneThreshold=100
///   => pruneTarget = 5 - 2 = 3, gap = 3 &lt; 100 => pruning skipped
/// </summary>
public sealed class BlockPruningThresholdTests : BlockPruningThresholdTestBase
{
    private readonly IBlockPruningInfoManager _blockPruningInfoManager;
    private readonly IBlockPruningService _blockPruningService;
    private readonly IBlockManager _blockManager;
    private readonly IChainManager _chainManager;

    public BlockPruningThresholdTests()
    {
        _blockPruningService = GetRequiredService<IBlockPruningService>();
        _blockPruningInfoManager = GetRequiredService<IBlockPruningInfoManager>();
        _blockManager = GetRequiredService<IBlockManager>();
        _chainManager = GetRequiredService<IChainManager>();
    }

    [Fact]
    public async Task PruneBlockchainData_BelowThreshold_ShouldSkip_Test()
    {
        await _blockPruningService.PruneBlockchainDataAsync();

        var lastPruned = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        lastPruned.ShouldBe(0);

        for (var h = 2L; h <= 3; h++)
        {
            var idx = await _chainManager.GetChainBlockIndexAsync(h);
            idx.ShouldNotBeNull();
            (await _blockManager.GetBlockAsync(idx.BlockHash)).ShouldNotBeNull();
        }
    }
}

/// <summary>
/// Fault injection: simulate crash-recovery scenarios where block data is partially deleted.
/// Uses BlockPruningServiceTestModule: Enabled=true, RetainDistance=2, pruneTarget=3
/// </summary>
public sealed class BlockPruningFaultInjectionTests : BlockPruningServiceTestBase
{
    private readonly IBlockManager _blockManager;
    private readonly IBlockPruningInfoManager _blockPruningInfoManager;
    private readonly IBlockPruningService _blockPruningService;
    private readonly IChainManager _chainManager;

    public BlockPruningFaultInjectionTests()
    {
        _blockPruningService = GetRequiredService<IBlockPruningService>();
        _blockPruningInfoManager = GetRequiredService<IBlockPruningInfoManager>();
        _blockManager = GetRequiredService<IBlockManager>();
        _chainManager = GetRequiredService<IChainManager>();
    }

    [Fact]
    public async Task PruneBlockchainData_BlockAlreadyDeleted_ShouldRecover_Test()
    {
        var idx = await _chainManager.GetChainBlockIndexAsync(2);
        idx.ShouldNotBeNull();
        await _blockManager.RemoveBlockAsync(idx.BlockHash);

        await _blockPruningService.PruneBlockchainDataAsync();

        var lastPruned = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        lastPruned.ShouldBe(3);
    }

    [Fact]
    public async Task PruneBlockchainData_AllBlocksAlreadyDeleted_ShouldComplete_Test()
    {
        for (var h = 2L; h <= 3; h++)
        {
            var idx = await _chainManager.GetChainBlockIndexAsync(h);
            if (idx != null)
                await _blockManager.RemoveBlockAsync(idx.BlockHash);
        }

        await _blockPruningService.PruneBlockchainDataAsync();

        var lastPruned = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        lastPruned.ShouldBe(3);
    }

    [Fact]
    public async Task PruneBlockchainData_ChainBlockLinkAlreadyDeleted_ShouldComplete_Test()
    {
        var idx = await _chainManager.GetChainBlockIndexAsync(2);
        idx.ShouldNotBeNull();
        await _chainManager.RemoveChainBlockLinkAsync(idx.BlockHash);

        await _blockPruningService.PruneBlockchainDataAsync();

        var lastPruned = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        lastPruned.ShouldBe(3);
    }

    [Fact]
    public async Task PruneBlockchainData_ResumeAfterPartialPrune_ShouldConverge_Test()
    {
        await _blockPruningService.PruneBlockchainDataAsync();
        var firstPruned = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        firstPruned.ShouldBe(3);

        await _blockPruningInfoManager.SetLastPrunedHeightAsync(1);

        await _blockPruningService.PruneBlockchainDataAsync();

        var secondPruned = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        secondPruned.ShouldBe(3);
    }
}

public sealed class BlockPruningDisabledTests : BlockPruningDisabledTestBase
{
    private readonly IBlockManager _blockManager;
    private readonly IBlockPruningInfoManager _blockPruningInfoManager;
    private readonly IBlockPruningService _blockPruningService;
    private readonly IChainManager _chainManager;

    public BlockPruningDisabledTests()
    {
        _blockPruningService = GetRequiredService<IBlockPruningService>();
        _blockPruningInfoManager = GetRequiredService<IBlockPruningInfoManager>();
        _blockManager = GetRequiredService<IBlockManager>();
        _chainManager = GetRequiredService<IChainManager>();
    }

    [Fact]
    public async Task PruneBlockchainData_Disabled_NoOp_Test()
    {
        await _blockPruningService.PruneBlockchainDataAsync();

        var lastPruned = await _blockPruningInfoManager.GetLastPrunedHeightAsync();
        lastPruned.ShouldBe(0);

        for (var h = 2L; h <= 5; h++)
        {
            var idx = await _chainManager.GetChainBlockIndexAsync(h);
            idx.ShouldNotBeNull();
            (await _blockManager.GetBlockAsync(idx.BlockHash)).ShouldNotBeNull();
        }
    }
}
