using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Managers;
using IChainManager = AElf.Kernel.Managers.Another.IChainManager;

namespace AElf.Kernel.Services
{
    public interface ILightBlockchainService
    {
        Task<bool> HasBlockHeaderAsync(int chainId, Hash blockHash);
        Task AddBlockHeadersAsync(int chainId, IEnumerable<IBlockHeader> headers);
        Task<Chain> GetChainAsync(int chainId);
        Task<IBlockHeader> GetBlockHeaderByHashAsync(int chainId, Hash blockHash);
        Task<IBlockHeader> GetIrreversibleBlockHeaderByHeightAsync(int chainId, long height);
    }
    
    public class LightBlockchainService: ILightBlockchainService
    {
        private readonly IBlockManager _blockManager;
        private readonly IChainManager _chainManager;

        public LightBlockchainService(IBlockManager blockManager)
        {
            _blockManager = blockManager;
        }

        public async Task<bool> HasBlockHeaderAsync(int chainId, Hash blockHash)
        {
            throw new System.NotImplementedException();
        }

        public async Task AddBlockHeadersAsync(int chainId, IEnumerable<IBlockHeader> headers)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Chain> GetChainAsync(int chainId)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IBlockHeader> GetBlockHeaderByHashAsync(int chainId, Hash blockHash)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IBlockHeader> GetIrreversibleBlockHeaderByHeightAsync(int chainId, long height)
        {
            throw new System.NotImplementedException();
        }
    }
}