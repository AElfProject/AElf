using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Managers;
using IChainManager = AElf.Kernel.Managers.Another.IChainManager;

namespace AElf.Kernel.Services
{
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
        private readonly IBlockManager _blockManager;
        private readonly IChainManager _chainManager;

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
            throw new System.NotImplementedException();
        }
    }
}