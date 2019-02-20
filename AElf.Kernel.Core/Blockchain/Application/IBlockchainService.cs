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
using IChainManager = AElf.Kernel.Blockchain.Domain.IChainManager;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockchainService
    {
        Task<Chain> CreateChainAsync(int chainId, Block block);
        Task AddBlockAsync(int chainId, Block block);
        Task<bool> HasBlockAsync(int chainId, Hash blockId);
        Task<List<ChainBlockLink>> AttachBlockToChainAsync(Chain chain, Block block);
        Task<Block> GetBlockByHashAsync(int chainId, Hash blockId);
        Task<Chain> GetChainAsync(int chainId);
        Task<Block> GetBlockByHeightAsync(int chainId, ulong height);
        Task<List<Hash>> GetBlockHeaders(int chainId, Hash firstHash, int count);
        Task<BlockHeader> GetBestChainLastBlock(int chainId);
        Task<Hash> GetBlockHashByHeightAsync(Chain chain, ulong height, Hash currentBlockHash = null);
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
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly ITransactionManager _transactionManager;
        private readonly IBlockValidationService _blockValidationService;
        public ILocalEventBus LocalEventBus { get; set; }
        public ILogger<FullBlockchainService> Logger { get; set; }

        public FullBlockchainService(IChainManager chainManager, IBlockManager blockManager,
            IBlockExecutingService blockExecutingService, ITransactionManager transactionManager, 
            IBlockValidationService blockValidationService)
        {
            Logger = NullLogger<FullBlockchainService>.Instance;
            _chainManager = chainManager;
            _blockManager = blockManager;
            _blockExecutingService = blockExecutingService;
            _transactionManager = transactionManager;
            _blockValidationService = blockValidationService;
            LocalEventBus = NullLocalEventBus.Instance;
        }
        
        public async Task<Chain> CreateChainAsync(int chainId, Block block)
        {
            await AddBlockAsync(chainId, block);
            return await _chainManager.CreateAsync(chainId, block.GetHash());
        }

        public async Task AddBlockAsync(int chainId, Block block)
        {
            await _blockManager.AddBlockHeaderAsync(block.Header);
            if (block.Body.TransactionList.Count > 0)
            {
                foreach (var transaction in block.Body.TransactionList)
                {
//                    await _transactionManager.AddTransactionAsync(transaction);
                }
            }

            await _blockManager.AddBlockBodyAsync(block.Header.GetHash(), block.Body);
        }

        public async Task<bool> HasBlockAsync(int chainId, Hash blockId)
        {
            return (await _blockManager.GetBlockHeaderAsync(blockId)) != null;
        }

        public async Task<List<ChainBlockLink>> AttachBlockToChainAsync(Chain chain, Block block)
        {
            var status = await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
            {
                Height = block.Header.Height,
                BlockHash = block.Header.GetHash(),
                PreviousBlockHash = block.Header.PreviousBlockHash
            });
            
            List<ChainBlockLink> blockLinks = null;

            if (status.HasFlag(BlockAttachOperationStatus.NewBlockLinked))
            {
                blockLinks = await _chainManager.GetNotExecutedBlocks(chain.Id, block.Header.GetHash());

                foreach (var blockLink in blockLinks)
                {
                    if (!await _blockValidationService.ValidateBlockBeforeExecuteAsync(chain.Id, block))
                    {
                        Logger.LogWarning($"Block validate fails before execution. BlockLink hash : {blockLink.BlockHash}");
                        break;
                    }

                    await ExecuteBlock(chain.Id, blockLink);

                    if (!await _blockValidationService.ValidateBlockAfterExecuteAsync(chain.Id, block))
                    {
                        Logger.LogWarning($"Block validate fails after execution. BlockLink hash : {blockLink.BlockHash}");
                        break;
                    }
                    
                    _chainManager.SetChainBlockLinkAsValidated(chain.Id, blockLink);
                }
                
                var lastBlockLink = blockLinks.LastOrDefault();
                if (lastBlockLink != null && lastBlockLink.IsExecuted && lastBlockLink.IsValidated)
                {
                    if (lastBlockLink.Height > chain.BestChainHeight)
                    {
                        chain.BestChainHeight = lastBlockLink.Height;
                        chain.BestChainHash = lastBlockLink.BlockHash;
                        _chainManager.SetAsync(chain.Id, chain);
                        
                        await LocalEventBus.PublishAsync(
                            new BestChainFoundEvent()
                            {
                                ChainId = chain.Id,
                                BlockHash = chain.BestChainHash,
                                BlockHeight = chain.BestChainHeight
                            });
                    }
                }
            }

            return blockLinks;
        }

        private async Task ExecuteBlock(int chainId, ChainBlockLink blockLink)
        {
            var block = await GetBlockByHashAsync(chainId, blockLink.BlockHash);
            // TODO: Save transactions in block
            var result =
                await _blockExecutingService.ExecuteBlockAsync(chainId, block.Header, block.Body.TransactionList);
            if (!result.GetHash().Equals(block.GetHash()))
            {
                // TODO: execution resulted a different hash, so the result is not correct
            }
            else
            {
                await _chainManager.SetChainBlockLinkAsExecuted(chainId, blockLink);
            }
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
        
        public async Task<List<Hash>> GetBlockHeaders(int chainId, Hash firstHash, int count)
        {
            var chain = await GetChainAsync(chainId);
            var first = await _blockManager.GetBlockHeaderAsync(firstHash);

            if (first == null)
                return null;

            var hashes = new List<Hash>();
                
            for (ulong i = first.Height-1; i >= first.Height - (ulong)count && i > 0; i--)
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
            return await _blockManager.GetBlockAsync(blockId);
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