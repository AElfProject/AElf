using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Events;
using AElf.Kernel.Managers;
using AElf.Kernel.Managers.Another;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using IChainManager = AElf.Kernel.Managers.Another.IChainManager;

namespace AElf.Kernel.Services
{
    public interface IBlockchainService
    {
        Task AddBlockAsync(int chainId, Block block);
        Task<bool> HasBlockAsync(int chainId, Hash blockId);
        Task<List<ChainBlockLink>> AttachBlockToChainAsync(Chain chain, Block block);
        Task<Block> GetBlockByHashAsync(int chainId, Hash blockId);
        Task<Chain> GetChainAsync(int chainId);
        Task<Block> GetBlockByHeightAsync(int chainId, ulong height);
        Task<List<BlockHeader>> GetBlockHeaders(int chainId, Hash firstHash, int count);
        Task<BlockHeader> GetBestChainLastBlock(int chainId);
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
        public ILocalEventBus LocalEventBus { get; set; }

        public FullBlockchainService(IChainManager chainManager, IBlockManager blockManager,
            IBlockExecutingService blockExecutingService)
        {
            _chainManager = chainManager;
            _blockManager = blockManager;
            _blockExecutingService = blockExecutingService;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public async Task AddBlockAsync(int chainId, Block block)
        {
            await _blockManager.AddBlockHeaderAsync(block.Header);
            await _blockManager.AddBlockBodyAsync(block.Header.GetHash(), block.Body);
        }

        public async Task<bool> HasBlockAsync(int chainId, Hash blockId)
        {
            return (await _blockManager.GetBlockHeaderAsync(blockId)) != null;
        }

        public async Task<List<BlockHeader>> GetBlockHeaders(int chainId, Hash firstHash, int count)
        {
            var first = await _blockManager.GetBlockHeaderAsync(firstHash);

            if (first == null)
                return null;

            var headers = new List<BlockHeader>();
                
            for (ulong i = first.Height-1; i >= first.Height - (ulong)count && i > 0; i--)
            {
                var bHeader = await GetBlockHeaderByHeightAsync(chainId, i);

                if (bHeader == null)
                    return headers;
                
                headers.Add(bHeader);
            }

            return headers;
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
                    await ExecuteBlock(chain.Id, blockLink);
                }

                if (status.HasFlag(BlockAttachOperationStatus.BestChainFound))
                {
                    await LocalEventBus.PublishAsync(
                        new BestChainFoundEvent()
                        {
                            ChainId = chain.Id,
                            BlockHash = chain.BestChainHash,
                            BlockHeight = chain.BestChainHeight
                        });
                }
            }

            return blockLinks;
        }

        private async Task ExecuteBlock(int chainId, ChainBlockLink blockLink)
        {
            await _blockExecutingService.ExecuteBlockAsync(chainId, blockLink.BlockHash);
            await _chainManager.SetChainBlockLinkAsExecuted(chainId, blockLink);
        }

        public async Task<Block> GetBlockByHashAsync(int chainId, Hash blockId)
        {
            return await _blockManager.GetBlockAsync(blockId);
        }

        public async Task<Block> GetBlockByHeightAsync(int chainId, ulong height)
        {
            var index = await _chainManager.GetChainBlockIndexAsync(chainId, height);
            return await GetBlockByHashAsync(chainId, index.BlockHash);
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