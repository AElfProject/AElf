using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IChainManager
    {
        /// <summary>
        /// append given block to one chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        Task AppendBlockToChainAsync(Hash chainId, IBlock block);

        /// <summary>
        /// append given header to one chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        Task AppendBlockHeaderAsync(Hash chainId, BlockHeader header);
        
        Task<IChain> GetChainAsync(Hash id);
        Task<IChain> AddChainAsync(Hash chainId, Hash genesisBlockHash);
        
        /// <summary>
        /// get height for one chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task<ulong> GetChainCurrentHeightAsync(Hash chainId);

        /// <summary>
        /// set height for one chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        Task SetChainCurrentHeightAsync(Hash chainId, ulong height);
        
        /// <summary>
        /// get last block hash for one chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task<Hash> GetChainLastBlockHashAsync(Hash chainId);

        /// <summary>
        /// set height for one chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        Task SetChainLastBlockHashAsync(Hash chainId, Hash blockHash);
    }
} 