﻿using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Managers
{
    public interface IChainManager
    {
        /// <summary>
        /// append given block to one chain
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        Task<bool> AppendBlockToChainAsync(IBlock block);

        /// <summary>
        /// append given header to one chain
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        Task AppendBlockHeaderAsync(BlockHeader header);

        Task<bool> Exists(Hash chainId);
        Task<IChain> GetChainAsync(Hash id);
        Task<IChain> AddChainAsync(Hash chainId, Hash genesisBlockHash);
        
        /// <summary>
        /// get height for one chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task<ulong> GetChainCurrentHeight(Hash chainId);

        /// <summary>
        /// set height for one chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        Task SetChainCurrentHeight(Hash chainId, ulong height);
        
        /// <summary>
        /// get last block hash for one chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task<Hash> GetChainLastBlockHash(Hash chainId);

        /// <summary>
        /// set height for one chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        Task SetChainLastBlockHash(Hash chainId, Hash blockHash);
    }
} 