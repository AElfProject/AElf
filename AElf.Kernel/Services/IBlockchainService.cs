using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Managers;
using IChainManager = AElf.Kernel.Managers.Another.IChainManager;

namespace AElf.Kernel.Services
{
    public interface IBlockchainServiceBase
    {
        Task<bool> AddBlockAsync(int chainId, Block block);
        
    }
    
    public interface IBlockchainService: ILightBlockchainService
    {
        Task<bool> HasBlockAsync(int chainId, Hash blockId);
        Task AddBlocksAsync(int chainId, IEnumerable<Block> blocks);
        Task<Block> GetBlockByHashAsync(int chainId, Hash blockId, bool withTransaction = false);
        Task<Block> GetBlockByHeightAsync(int chainId, long height, bool withTransaction = false);
    }

    public class BlockchainService : LightBlockchainService, IBlockchainService
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
    }
}