using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Extensions;

public class BlockchainServiceExtensionsTests : OSCoreWithChainTestBase
{
    private readonly IBlockchainService _blockchainService;
    private readonly IBlockManager _blockManager;

    public BlockchainServiceExtensionsTests()
    {
        _blockchainService = GetRequiredService<IBlockchainService>();
        _blockManager = GetRequiredService<IBlockManager>();
    }

    [Fact]
    public async Task GetBlocksWithTransactions_AllBlocksExist_Test()
    {
        var chain = await _blockchainService.GetChainAsync();
        var genesisHash = chain.GenesisBlockHash;

        var result = await _blockchainService.GetBlocksWithTransactionsAsync(genesisHash, 3);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        foreach (var bwt in result)
        {
            bwt.Header.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GetBlocksWithTransactions_MiddleBlockDeleted_ShouldTruncate_Test()
    {
        var chain = await _blockchainService.GetChainAsync();
        var genesisHash = chain.GenesisBlockHash;

        var blocks = await _blockchainService.GetBlocksInBestChainBranchAsync(genesisHash, 5);
        blocks.Count.ShouldBeGreaterThanOrEqualTo(5);

        await _blockManager.RemoveBlockAsync(blocks[2].GetHash());

        var result = await _blockchainService.GetBlocksWithTransactionsAsync(genesisHash, 5);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetBlocksWithTransactions_FirstBlockDeleted_ShouldReturnEmpty_Test()
    {
        var chain = await _blockchainService.GetChainAsync();
        var genesisHash = chain.GenesisBlockHash;

        var blocks = await _blockchainService.GetBlocksInBestChainBranchAsync(genesisHash, 3);
        blocks.Count.ShouldBeGreaterThanOrEqualTo(3);

        await _blockManager.RemoveBlockAsync(blocks[0].GetHash());

        var result = await _blockchainService.GetBlocksWithTransactionsAsync(genesisHash, 3);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetBlockWithTransactionsByHash_BlockExists_Test()
    {
        var chain = await _blockchainService.GetChainAsync();
        var block = await _blockchainService.GetBlockByHashAsync(chain.GenesisBlockHash);
        block.ShouldNotBeNull();

        var result = await _blockchainService.GetBlockWithTransactionsByHashAsync(chain.GenesisBlockHash);

        result.ShouldNotBeNull();
        result.Header.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetBlockWithTransactionsByHash_BlockNotExist_ShouldReturnNull_Test()
    {
        var result = await _blockchainService.GetBlockWithTransactionsByHashAsync(
            HashHelper.ComputeFrom("nonexistent"));

        result.ShouldBeNull();
    }
}
