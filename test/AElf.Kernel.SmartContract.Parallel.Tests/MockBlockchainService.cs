using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class MockBlockchainService : IBlockchainService
    {
        private List<Transaction> _data = new List<Transaction>();
        
        public int GetChainId()
        {
            throw new System.NotImplementedException();
        }

        public async Task<Chain> CreateChainAsync(Block block, IEnumerable<Transaction> transactions)
        {
            throw new System.NotImplementedException();
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

        public async Task AddBlockAsync(Block block)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> HasBlockAsync(Hash blockId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Block> GetBlockByHashAsync(Hash blockId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<BlockHeader> GetBlockHeaderByHashAsync(Hash blockId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Chain> GetChainAsync()
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<IBlockIndex>> GetReversedBlockIndexes(Hash lastBlockHash, int count)
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<Hash>> GetBlockHashesAsync(Chain chain, Hash firstHash, int count, Hash chainBranchBlockHash = null)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Hash> GetBlockHashByHeightAsync(Chain chain, long height, Hash chainBranchBlockHash)
        {
            throw new System.NotImplementedException();
        }

        public async Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, Block block)
        {
            throw new System.NotImplementedException();
        }

        public async Task SetBestChainAsync(Chain chain, long bestChainHeight, Hash bestChainHash)
        {
            throw new System.NotImplementedException();
        }

        public async Task SetIrreversibleBlockAsync(Chain chain, long irreversibleBlockHeight, Hash irreversibleBlockHash)
        {
            throw new System.NotImplementedException();
        }
    }
}