using System;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Blockchain.Domain;

public interface IBlockManager
{
    Task AddBlockHeaderAsync(BlockHeader header);
    Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody);
    Task<Block> GetBlockAsync(Hash blockHash);
    Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash);
    Task RemoveBlockAsync(Hash blockHash);
    Task<bool> HasBlockAsync(Hash blockHash);
}

public class BlockManager : IBlockManager
{
    private readonly IBlockchainStore<BlockBody> _blockBodyStore;
    private readonly IBlockchainStore<BlockHeader> _blockHeaderStore;

    public BlockManager(IBlockchainStore<BlockHeader> blockHeaderStore, IBlockchainStore<BlockBody> blockBodyStore)
    {
        Logger = NullLogger<BlockManager>.Instance;
        _blockHeaderStore = blockHeaderStore;
        _blockBodyStore = blockBodyStore;
    }

    public ILogger<BlockManager> Logger { get; set; }

    public async Task AddBlockHeaderAsync(BlockHeader header)
    {
        await _blockHeaderStore.SetAsync(header.GetHash().ToStorageKey(), header);
    }

    public async Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody)
    {
        await _blockBodyStore.SetAsync(blockHash.Clone().ToStorageKey(), blockBody);
    }

    public async Task<Block> GetBlockAsync(Hash blockHash)
    {
        if (blockHash == null) return null;

        try
        {
            var header = await GetBlockHeaderAsync(blockHash);
            var bb = await GetBlockBodyAsync(blockHash);

            if (header == null || bb == null)
                return null;

            return new Block { Header = header, Body = bb };
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error while getting block {BlockHash}", blockHash.ToHex());
            return null;
        }
    }

    public async Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash)
    {
        return await _blockHeaderStore.GetAsync(blockHash.ToStorageKey());
    }

    public async Task RemoveBlockAsync(Hash blockHash)
    {
        var blockKey = blockHash.ToStorageKey();
        await _blockHeaderStore.RemoveAsync(blockKey);
        await _blockBodyStore.RemoveAsync(blockKey);
    }

    public async Task<bool> HasBlockAsync(Hash blockHash)
    {
        return await _blockHeaderStore.IsExistsAsync(blockHash.ToStorageKey());
    }

    private async Task<BlockBody> GetBlockBodyAsync(Hash bodyHash)
    {
        return await _blockBodyStore.GetAsync(bodyHash.ToStorageKey());
    }
}