using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockchainService
    {
        int GetChainId();
        
        Task<Chain> CreateChainAsync(Block block, IEnumerable<Transaction> transactions);
        Task AddTransactionsAsync(IEnumerable<Transaction> transactions);
        Task<List<Transaction>> GetTransactionsAsync(IEnumerable<Hash> transactionIds);
        Task AddBlockAsync(Block block);
        Task<bool> HasBlockAsync(Hash blockId);
        Task<Block> GetBlockByHashAsync(Hash blockId);

        Task<BlockHeader> GetBlockHeaderByHashAsync(Hash blockId);

        Task<Chain> GetChainAsync();

        Task<List<IBlockIndex>> GetReversedBlockIndexes(Hash lastBlockHash, int count);

        /// <summary>
        /// if to chainBranchBlockHash have no enough block hashes, may return less hashes than count
        /// for example, chainBranchBlockHash's height is 5, count is 10, firstHash's height is 2, will only return
        /// hashes 3, 4, 5
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="firstHash"></param>
        /// <param name="count"></param>
        /// <param name="chainBranchBlockHash"></param>
        /// <returns></returns>
        Task<List<Hash>> GetBlockHashesAsync(Chain chain, Hash firstHash, int count,
            Hash chainBranchBlockHash = null);

        Task<Hash> GetBlockHashByHeightAsync(Chain chain, long height, Hash chainBranchBlockHash);
        Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, Block block);
        Task SetBestChainAsync(Chain chain, long bestChainHeight, Hash bestChainHash);
        Task SetIrreversibleBlockAsync(Chain chain, long irreversibleBlockHeight, Hash irreversibleBlockHash);
    }

    public static class BlockchainServiceExtensions
    {
        public static async Task<BlockHeader> GetBestChainLastBlockHeaderAsync(
            this IBlockchainService blockchainService)
        {
            var chain = await blockchainService.GetChainAsync();
            return await blockchainService.GetBlockHeaderByHashAsync(chain.BestChainHash);
        }

        public static async Task<Block> GetBlockByHeightInBestChainBranchAsync(
            this IBlockchainService blockchainService, long height)
        {
            var chain = await blockchainService.GetChainAsync();
            var hash = await blockchainService.GetBlockHashByHeightAsync(chain, height, chain.BestChainHash);

            return await blockchainService.GetBlockByHashAsync(hash);
        }

        public static async Task<List<Block>> GetBlocksInBestChainBranchAsync(this IBlockchainService blockchainService,
            Hash firstHash,
            int count)
        {
            var chain = await blockchainService.GetChainAsync();
            return await blockchainService.GetBlocksInChainBranchAsync(chain, firstHash, count, chain.BestChainHash);
        }

        public static async Task<List<Block>> GetBlocksInLongestChainBranchAsync(
            this IBlockchainService blockchainService,
            Hash firstHash,
            int count)
        {
            var chain = await blockchainService.GetChainAsync();
            return await blockchainService.GetBlocksInChainBranchAsync(chain, firstHash, count, chain.LongestChainHash);
        }

        public static async Task<List<Block>> GetBlocksInChainBranchAsync(this IBlockchainService blockchainService,
            Chain chain,
            Hash firstHash,
            int count,
            Hash chainBranch)
        {
            var blockHashes = await blockchainService.GetBlockHashesAsync(
                chain, firstHash, count, chainBranch);
            var list = blockHashes
                .Select(async blockHash => await blockchainService.GetBlockByHashAsync(blockHash));

            return (await Task.WhenAll(list)).ToList();
        }
    }

    public interface ILightBlockchainService : IBlockchainService
    {
    }

    /*
    public class LightBlockchainService : ILightBlockchainService
    {
        public async Task<bool> AddBlockAsync( Block block)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> HasBlockAsync( Hash blockId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<ChainBlockLink>> AddBlocksAsync( IEnumerable<Block> blocks)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Block> GetBlockByHashAsync( Hash blockId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Chain> GetChainAsync()
        {
            throw new System.NotImplementedException();
        }
    }*/

    public interface IFullBlockchainService : IBlockchainService
    {
    }

    public class FullBlockchainService : IFullBlockchainService, ITransientDependency
    {
        private readonly IChainManager _chainManager;
        private readonly IBlockManager _blockManager;
        private readonly ITransactionManager _transactionManager;
        public ILocalEventBus LocalEventBus { get; set; }
        public ILogger<FullBlockchainService> Logger { get; set; }

        public FullBlockchainService(IChainManager chainManager, IBlockManager blockManager,
            ITransactionManager transactionManager)
        {
            Logger = NullLogger<FullBlockchainService>.Instance;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _transactionManager = transactionManager;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public int GetChainId()
        {
            return _chainManager.GetChainId();
        }

        public async Task<Chain> CreateChainAsync(Block block, IEnumerable<Transaction> transactions)
        {
            await AddTransactionsAsync(transactions);
            await AddBlockAsync(block);
            
            return await _chainManager.CreateAsync(block.GetHash());;
        }

        public async Task<List<Transaction>> GetTransactionsAsync(IEnumerable<Hash> transactionIds)
        {
            List<Transaction> transactions = new List<Transaction>();
            
            foreach (var transactionId in transactionIds)
            {
                var transaction = await _transactionManager.GetTransaction(transactionId);
                transactions.Add(transaction);
            }

            return transactions;
        }

        public async Task AddBlockAsync(Block block)
        {
            await _blockManager.AddBlockBodyAsync(block.Header.GetHash(), block.Body);
            await _blockManager.AddBlockHeaderAsync(block.Header);
        }

        public async Task AddTransactionsAsync(IEnumerable<Transaction> transactions)
        {
            foreach (var transaction in transactions)
                await _transactionManager.AddTransactionAsync(transaction);
        }

        public async Task<bool> HasBlockAsync(Hash blockId)
        {
            return (await _blockManager.GetBlockHeaderAsync(blockId)) != null;
        }

        /// <summary>
        /// Returns the block with the specified height, searching from <see cref="chainBranchBlockHash"/>. If the height
        /// is in the irreversible section of the chain, it will get the block from the indexed blocks.
        /// </summary>
        /// <param name="chain">the chain to search</param>
        /// <param name="height">the height of the block</param>
        /// <param name="chainBranchBlockHash">the block from which to start the search.</param>
        /// <returns></returns>
        public async Task<Hash> GetBlockHashByHeightAsync(Chain chain, long height, Hash chainBranchBlockHash)
        {
            // 1 2 3 4(lib) 5 6
            // 4 >= height
            // return any(1,2,3,4)

            if (chain.LastIrreversibleBlockHeight >= height)
            {
                // search irreversible section of the chain
                return (await _chainManager.GetChainBlockIndexAsync(height)).BlockHash;
            }

            // Last irreversible block can make the loops in acceptable size, do not need cache 

            // 1 2 3 4(lib) 5 6
            // 4 < height
            // return any(5,6)


            var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(chainBranchBlockHash);
            if (chainBlockLink.Height < height)
            {
                Logger.LogWarning($"Start searching height: {chainBlockLink.Height},target height: {height},cannot get block hash");
                return null; 
            } 
            while (true)
            {
                if (chainBlockLink.Height == height)
                    return chainBlockLink.BlockHash;

                chainBranchBlockHash = chainBlockLink.PreviousBlockHash;
                chainBlockLink = await _chainManager.GetChainBlockLinkAsync(chainBranchBlockHash);

                if (chainBlockLink == null || chainBlockLink.Height <= chain.LastIrreversibleBlockHeight)
                    return null;
            }
        }

        public async Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, Block block)
        {
            var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(block.GetHash());

            if (chainBlockLink == null)
            {
                chainBlockLink = new ChainBlockLink
                {
                    Height = block.Header.Height,
                    BlockHash = block.GetHash(),
                    PreviousBlockHash = block.Header.PreviousBlockHash
                };
            }
            else
            {
                chainBlockLink.IsLinked = false;
                chainBlockLink.ExecutionStatus =
                    chainBlockLink.ExecutionStatus == ChainBlockLinkExecutionStatus.ExecutionSuccess
                        ? ChainBlockLinkExecutionStatus.ExecutionSuccess
                        : ChainBlockLinkExecutionStatus.ExecutionNone;
            }

            var status = await _chainManager.AttachBlockToChainAsync(chain, chainBlockLink);

            return status;
        }

        public async Task SetBestChainAsync(Chain chain, long bestChainHeight, Hash bestChainHash)
        {
            await _chainManager.SetBestChainAsync(chain, bestChainHeight, bestChainHash);
        }

        public async Task SetIrreversibleBlockAsync(Chain chain, long irreversibleBlockHeight,
            Hash irreversibleBlockHash)
        {
            // Create before IChainManager.SetIrreversibleBlockAsync so that we can correctly get the previous LIB info
            var eventDataToPublish = new NewIrreversibleBlockFoundEvent()
            {
                PreviousIrreversibleBlockHash = chain.LastIrreversibleBlockHash,
                PreviousIrreversibleBlockHeight = chain.LastIrreversibleBlockHeight,
                BlockHash = irreversibleBlockHash,
                BlockHeight = irreversibleBlockHeight
            };

            var success = await _chainManager.SetIrreversibleBlockAsync(chain, irreversibleBlockHash);
            if (!success) return;
            // TODO: move to background job, it will slow down our system
            // Clean last branches and not linked
            var toCleanBlocks = await _chainManager.CleanBranchesAsync(chain, eventDataToPublish.PreviousIrreversibleBlockHash,
                eventDataToPublish.PreviousIrreversibleBlockHeight);
            await RemoveBlocksAsync(toCleanBlocks);

            await LocalEventBus.PublishAsync(eventDataToPublish);
        }

        public async Task<List<IBlockIndex>> GetReversedBlockIndexes(Hash lastBlockHash, int count)
        {
            var hashes = new List<IBlockIndex>();
            if (count <= 0)
                return hashes;

            var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(lastBlockHash);
            if (chainBlockLink == null || chainBlockLink.PreviousBlockHash == Hash.Empty)
                return hashes;

            hashes.Add(new BlockIndex(chainBlockLink.PreviousBlockHash, chainBlockLink.Height - 1));
            for (var i = 0; i < count - 1; i++)
            {
                chainBlockLink = await _chainManager.GetChainBlockLinkAsync(chainBlockLink.PreviousBlockHash);
                if (chainBlockLink == null || chainBlockLink.PreviousBlockHash == Hash.Empty)
                    break;

                hashes.Add(new BlockIndex(chainBlockLink.PreviousBlockHash, chainBlockLink.Height - 1));
            }

            return hashes;
        }

        public async Task<List<Hash>> GetBlockHashesAsync(Chain chain, Hash firstHash, int count,
            Hash chainBranchBlockHash)
        {
            var first = await _blockManager.GetBlockHeaderAsync(firstHash);

            if (first == null)
                return new List<Hash>();

            var height = first.Height + count;

            var chainBranchBlockHashKey = chainBranchBlockHash.ToStorageKey();

            ChainBlockLink chainBlockLink = null;

            if (chain.Branches.ContainsKey(chainBranchBlockHashKey))
            {
                if (height > chain.Branches[chainBranchBlockHashKey])
                {
                    height = chain.Branches[chainBranchBlockHashKey];
                    count = (int) (height - first.Height);
                }
            }
            else
            {
                var branchChainBlockLink = await _chainManager.GetChainBlockLinkAsync(chainBranchBlockHash);
                if (height > branchChainBlockLink.Height)
                {
                    height = branchChainBlockLink.Height;
                    chainBlockLink = branchChainBlockLink;
                    count = (int) (height - first.Height);
                }
            }

            var hashes = new List<Hash>();

            if (count <= 0)
                return hashes;


            if (chainBlockLink == null)
            {
                var last = await GetBlockHashByHeightAsync(chain, height, chainBranchBlockHash);

                if (last == null)
                    throw new InvalidOperationException("not support");

                chainBlockLink = await _chainManager.GetChainBlockLinkAsync(last);
            }


            hashes.Add(chainBlockLink.BlockHash);
            for (var i = 0; i < count - 1; i++)
            {
                chainBlockLink = await _chainManager.GetChainBlockLinkAsync(chainBlockLink.PreviousBlockHash);
                hashes.Add(chainBlockLink.BlockHash);
            }

            if (chainBlockLink.PreviousBlockHash != firstHash)
            {
                //TODO need to improve
                throw new Exception("wrong branch");
            }

            hashes.Reverse();

            return hashes;
        }

        public async Task<Block> GetBlockByHashAsync(Hash blockId)
        {
            return await _blockManager.GetBlockAsync(blockId);;
        }

        public async Task<BlockHeader> GetBlockHeaderByHashAsync(Hash blockId)
        {
            return await _blockManager.GetBlockHeaderAsync(blockId);
        }

        public async Task<BlockHeader> GetBlockHeaderByHeightAsync(long height)
        {
            var index = await _chainManager.GetChainBlockIndexAsync(height);
            return await _blockManager.GetBlockHeaderAsync(index.BlockHash);
        }

        public async Task<Chain> GetChainAsync()
        {
            return await _chainManager.GetAsync();
        }

        private async Task RemoveBlocksAsync(List<Hash> blockHashes)
        {
            foreach (var blockHash in blockHashes)
            {
                await _chainManager.RemoveChainBlockLinkAsync(blockHash);
                await _blockManager.RemoveBlockAsync(blockHash);
            }
        }
    }
}