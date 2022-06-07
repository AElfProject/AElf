using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Tests;

public class MockBlockchainService : IBlockchainService
{
    private readonly List<Transaction> _data = new();

    public int GetChainId()
    {
        throw new NotImplementedException();
    }

    public async Task<Chain> CreateChainAsync(Block block, IEnumerable<Transaction> transactions)
    {
        return await Task.FromException<Chain>(new NotImplementedException());
    }

    public Task AddTransactionsAsync(IEnumerable<Transaction> transactions)
    {
        _data.AddRange(transactions);

        return Task.CompletedTask;
    }

    public Task<List<Transaction>> GetTransactionsAsync(IEnumerable<Hash> transactionHashes)
    {
        return Task.FromResult(_data.Where(d => transactionHashes.Contains(d.GetHash())).ToList());
    }

    public Task<bool> HasTransactionAsync(Hash transactionId)
    {
        throw new NotImplementedException();
    }

    public async Task AddBlockAsync(Block block)
    {
        await Task.FromException(new NotImplementedException());
    }

    public async Task<bool> HasBlockAsync(Hash blockId)
    {
        return await Task.FromException<bool>(new NotImplementedException());
    }

    public async Task<Block> GetBlockByHashAsync(Hash blockId)
    {
        return await Task.FromException<Block>(new NotImplementedException());
    }

    public async Task<BlockHeader> GetBlockHeaderByHashAsync(Hash blockId)
    {
        return await Task.FromException<BlockHeader>(new NotImplementedException());
    }

    public async Task<Chain> GetChainAsync()
    {
        return await Task.FromException<Chain>(new NotImplementedException());
    }

    public async Task<List<IBlockIndex>> GetReversedBlockIndexes(Hash lastBlockHash, int count)
    {
        return await Task.FromException<List<IBlockIndex>>(new NotImplementedException());
    }

    public async Task<List<Hash>> GetBlockHashesAsync(Chain chain, Hash firstHash, int count,
        Hash chainBranchBlockHash = null)
    {
        return await Task.FromException<List<Hash>>(new NotImplementedException());
    }

    public async Task<Hash> GetBlockHashByHeightAsync(Chain chain, long height, Hash chainBranchBlockHash)
    {
        return await Task.FromException<Hash>(new NotImplementedException());
    }

    public async Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, Block block)
    {
        return await Task.FromException<BlockAttachOperationStatus>(new NotImplementedException());
    }

    public async Task SetBestChainAsync(Chain chain, long bestChainHeight, Hash bestChainHash)
    {
        await Task.FromException(new NotImplementedException());
    }

    public async Task SetIrreversibleBlockAsync(Chain chain, long irreversibleBlockHeight, Hash irreversibleBlockHash)
    {
        await Task.FromException(new NotImplementedException());
    }

    public Task<DiscardedBranch> GetDiscardedBranchAsync(Chain chain)
    {
        throw new NotImplementedException();
    }

    public Task CleanChainBranchAsync(DiscardedBranch discardedBranch)
    {
        throw new NotImplementedException();
    }

    public Task<Chain> ResetChainToLibAsync(Chain chain)
    {
        throw new NotImplementedException();
    }

    public Task RemoveLongestBranchAsync(Chain chain)
    {
        throw new NotImplementedException();
    }
}