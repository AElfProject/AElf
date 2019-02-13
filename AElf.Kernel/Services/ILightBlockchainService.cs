using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Managers;
using IChainManager = AElf.Kernel.Managers.Another.IChainManager;

namespace AElf.Kernel.Services
{
    /*
    public interface ILightBlockchainService
    {
        Task<bool> HasBlockHeaderLinkedAsync(int chainId, Hash blockHash);
        Task AddBlockHeadersAsync(int chainId, IEnumerable<BlockHeader> headers);
        Task<Chain> GetChainAsync(int chainId);
        Task<IBlockHeader> GetBlockHeaderByHashAsync(int chainId, Hash blockHash);
        Task<IBlockHeader> GetIrreversibleBlockHeaderByHeightAsync(int chainId, long height);
    } 

    public class LightBlockchainService: ILightBlockchainService
    {
        protected readonly IBlockManager _blockManager;
        protected readonly IChainManager _chainManager;

        public LightBlockchainService(IBlockManager blockManager, IChainManager chainManager)
        {
            _blockManager = blockManager;
            _chainManager = chainManager;
        }

        public async Task<bool> HasBlockHeaderLinkedAsync(int chainId, Hash blockHash)
        {
            var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(chainId, blockHash);
            return chainBlockLink.IsLinked;
        }

        public async Task AddBlockHeadersAsync(int chainId, IEnumerable<BlockHeader> headers)
        {
            var chain = await _chainManager.GetAsync(chainId);
            foreach (var blockHeader in headers)
            {
                await _blockManager.AddBlockHeaderAsync(blockHeader);
                await _chainManager.AttachBlockToChainAsync(chain, new ChainBlockLink()
                {
                    Height = blockHeader.Height,
                    BlockHash = blockHeader.GetHash(),
                    PreviousBlockHash = blockHeader.PreviousBlockHash
                });
            }
        }
        
        public async Task<Chain> GetChainAsync(int chainId)
        {
            return await _chainManager.GetAsync(chainId);
        }

        public async Task<IBlockHeader> GetBlockHeaderByHashAsync(int chainId, Hash blockHash)
        {
            var header = await _blockManager.GetBlockHeaderAsync(blockHash);
            return header;
        }

        public async Task<IBlockHeader> GetIrreversibleBlockHeaderByHeightAsync(int chainId, long height)
        {
            var index = await _chainManager.GetChainBlockIndexAsync(chainId, height);
            return await _blockManager.GetBlockHeaderAsync(index.BlockHash);
        }
    }*/
    
    /*
    public interface IFullBlockchainService: ILightBlockchainService
    {
        Task<bool> HasBlockAsync(int chainId, Hash blockId);
        Task AddBlocksAsync(int chainId, IEnumerable<Block> blocks);
        Task<Block> GetBlockByHashAsync(int chainId, Hash blockId, bool withTransaction = false);
        Task<Block> GetBlockByHeightAsync(int chainId, long height, bool withTransaction = false);
    }

    public class BlockchainService : LightBlockchainService, IFullBlockchainService
    {
        public BlockchainService(IBlockManager blockManager, IChainManager chainManager) : base(blockManager, chainManager)
        {
        }

        public async Task<bool> HasBlockAsync(int chainId, Hash blockId)
        {
            throw new System.NotImplementedException();
        }

        public async Task AddBlocksAsync(int chainId, IEnumerable<Block> blocks)
        {
            
        }

        public async Task<Block> GetBlockByHashAsync(int chainId, Hash blockId, bool withTransaction = false)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Block> GetBlockByHeightAsync(int chainId, long height, bool withTransaction = false)
        {
            throw new System.NotImplementedException();
        }
    }*/
}