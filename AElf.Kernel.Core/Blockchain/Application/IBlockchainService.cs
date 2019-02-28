using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockchainService
    {
        Task<Chain> CreateChainAsync(int chainId, Block block);
        Task AddBlockAsync(int chainId, Block block);
        Task<bool> HasBlockAsync(int chainId, Hash blockId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="block"></param>
        /// <returns>blocks need to be executed</returns>
        //Task<List<ChainBlockLink>> AttachBlockToChainAsync(Chain chain, Block block);
        Task<Block> GetBlockByHashAsync(int chainId, Hash blockId);

        Task<BlockHeader> GetBlockHeaderByHashAsync(int chainId, Hash blockId);

        Task<Chain> GetChainAsync(int chainId);
        Task<Block> GetBlockByHeightAsync(int chainId, ulong height);
        Task<List<Hash>> GetBlockHeaders(int chainId, Hash firstHash, int count);
        Task<BlockHeader> GetBestChainLastBlock(int chainId);
        Task<Hash> GetBlockHashByHeightAsync(Chain chain, ulong height, Hash currentBlockHash = null);
        Task<BranchSwitch> GetBranchSwitchAsync(int chainId, Hash fromHash, Hash toHash);

        Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, Block block);
        Task SetBestChainAsync(Chain chain, ulong bestChainHeight, Hash bestChainHash);
    }

    public interface ILightBlockchainService : IBlockchainService
    {
    }

    /*
    public class LightBlockchainService : ILightBlockchainService
    {
        public async Task<bool> AddBlockAsync(int chainId, Block block)
        {
            throw new System.NotImplementedException();
        }

        public async Task<bool> HasBlockAsync(int chainId, Hash blockId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<List<ChainBlockLink>> AddBlocksAsync(int chainId, IEnumerable<Block> blocks)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Block> GetBlockByHashAsync(int chainId, Hash blockId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Chain> GetChainAsync(int chainId)
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

        public async Task<Chain> CreateChainAsync(int chainId, Block block)
        {
            await AddBlockAsync(chainId, block);
            var chain = await _chainManager.CreateAsync(chainId, block.GetHash());
            await LocalEventBus.PublishAsync(
                new BestChainFoundEventData()
                {
                    ChainId = chain.Id,
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                });
            return chain;
        }

        public async Task AddBlockAsync(int chainId, Block block)
        {
            await _blockManager.AddBlockHeaderAsync(block.Header);
            foreach (var transaction in block.Body.TransactionList)
            {
                await _transactionManager.AddTransactionAsync(transaction);
            }

            await _blockManager.AddBlockBodyAsync(block.Header.GetHash(), block.Body);
        }

        public async Task<bool> HasBlockAsync(int chainId, Hash blockId)
        {
            return (await _blockManager.GetBlockHeaderAsync(blockId)) != null;
        }


        /// <summary>
        /// Returns the block with the specified height, searching from <see cref="startBlockHash"/>. If the height
        /// is in the irreversible section of the chain, it will get the block from the indexed blocks.
        /// </summary>
        /// <param name="chain">the chain to search</param>
        /// <param name="height">the height of the block</param>
        /// <param name="startBlockHash">the block from which to start the search, by default the head of the best chain.</param>
        /// <returns></returns>
        public async Task<Hash> GetBlockHashByHeightAsync(Chain chain, ulong height, Hash startBlockHash = null)
        {
            if (chain.LastIrreversibleBlockHeight >= height)
            {
                // search irreversible section of the chain
                return (await _chainManager.GetChainBlockIndexAsync(chain.Id, height)).BlockHash;
            }

            if (startBlockHash == null)
                startBlockHash = chain.BestChainHash;

            // TODO: may introduce cache to improve the performance

            var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(chain.Id, startBlockHash);
            if (chainBlockLink.Height < height)
                return null;
            while (true)
            {
                if (chainBlockLink.Height == height)
                    return chainBlockLink.BlockHash;

                startBlockHash = chainBlockLink.PreviousBlockHash;
                chainBlockLink = await _chainManager.GetChainBlockLinkAsync(chain.Id, startBlockHash);
            }
        }

        public async Task<BranchSwitch> GetBranchSwitchAsync(int chainId, Hash fromHash, Hash toHash)
        {
            var fromGenesis = false;
            var fromHeader = await GetBlockHeaderByHashAsync(chainId, fromHash);
            var toHeader = await GetBlockHeaderByHashAsync(chainId, toHash);
            if (fromHeader == null && !(fromGenesis = fromHash == Hash.Genesis))
            {
                throw new Exception($"No block is found for provided from hash {fromHash}.");
            }

            if (toHeader == null)
            {
                throw new Exception($"No block is found for provided to hash {toHash}.");
            }

            var output = new BranchSwitch();
            var reversedNewBranch = new List<Hash>();
            checked
            {
                if (fromGenesis)
                {
                    for (int i = (int) toHeader.Height; i >= (int) ChainConsts.GenesisBlockHeight; i--)
                    {
                        reversedNewBranch.Add(toHeader.GetHash());
                        toHeader = await GetBlockHeaderByHashAsync(chainId, toHeader.PreviousBlockHash);
                    }
                }
                else
                {
                    for (ulong i = fromHeader.Height; i > toHeader.Height; i--)
                    {
                        output.RollBack.Add(fromHeader.GetHash());
                        fromHeader = await GetBlockHeaderByHashAsync(chainId, fromHeader.PreviousBlockHash);
                    }

                    for (ulong i = toHeader.Height; i > fromHeader.Height; i--)
                    {
                        reversedNewBranch.Add(toHeader.GetHash());
                        toHeader = await GetBlockHeaderByHashAsync(chainId, toHeader.PreviousBlockHash);
                    }

                    while (true)
                    {
                        var fh = fromHeader.GetHash();
                        var th = toHeader.GetHash();
                        if (fh == th)
                        {
                            break;
                        }

                        output.RollBack.Add(fh);
                        reversedNewBranch.Add(th);
                        fromHeader = await GetBlockHeaderByHashAsync(chainId, fromHeader.PreviousBlockHash);
                        toHeader = await GetBlockHeaderByHashAsync(chainId, toHeader.PreviousBlockHash);
                    }
                }
            }

            reversedNewBranch.Reverse();
            output.RollForward.AddRange(reversedNewBranch);
            return output;
        }

        public async Task<BlockAttachOperationStatus> AttachBlockToChainAsync(Chain chain, Block block)
        {
            var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = block.Header.Height,
                BlockHash = block.Header.GetHash(),
                PreviousBlockHash = block.Header.PreviousBlockHash
            });

            return status;
        }

        public async Task SetBestChainAsync(Chain chain, ulong bestChainHeight, Hash bestChainHash)
        {
            await _chainManager.SetBestChainAsync(chain, bestChainHeight, bestChainHash);
        }

        public async Task<List<Hash>> GetBlockHeaders(int chainId, Hash firstHash, int count)
        {
            var chain = await GetChainAsync(chainId);
            var first = await _blockManager.GetBlockHeaderAsync(firstHash);

            if (first == null)
                return null;

            var hashes = new List<Hash>();

            for (ulong i = first.Height - 1; hashes.Count < count && i > 0; i--)
            {
                var bHash = await GetBlockHashByHeightAsync(chain, i);

                if (bHash == null)
                    return hashes;

                hashes.Add(bHash);
            }

            return hashes;
        }

        public async Task<Block> GetBlockByHeightAsync(int chainId, ulong height)
        {
            var chain = await GetChainAsync(chainId);
            var hash = await GetBlockHashByHeightAsync(chain, height);

            return await GetBlockByHashAsync(chainId, hash);
        }

        public async Task<Block> GetBlockByHashAsync(int chainId, Hash blockId)
        {
            var block = await _blockManager.GetBlockAsync(blockId);
            if (block == null)
            {
                return null;
            }

            var body = block.Body;

            foreach (var txId in body.Transactions)
            {
                var tx = await _transactionManager.GetTransaction(txId);
                body.TransactionList.Add(tx);
            }

            return block;
        }

        public async Task<BlockHeader> GetBlockHeaderByHashAsync(int chainId, Hash blockId)
        {
            return await _blockManager.GetBlockHeaderAsync(blockId);
        }

        public async Task<BlockHeader> GetBlockHeaderByHeightAsync(int chainId, ulong height)
        {
            var index = await _chainManager.GetChainBlockIndexAsync(chainId, height);
            return await _blockManager.GetBlockHeaderAsync(index.BlockHash);
        }

        public async Task<Chain> GetChainAsync(int chainId)
        {
            return await _chainManager.GetAsync(chainId);
        }

        public async Task<BlockHeader> GetBestChainLastBlock(int chainId)
        {
            var chain = await GetChainAsync(chainId);
            return await _blockManager.GetBlockHeaderAsync(chain.BestChainHash);
        }
    }
}