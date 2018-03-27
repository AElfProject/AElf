﻿using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IChainManager
    {
        Task AppenBlockToChainAsync(Chain chain, Block block);
        Task<Chain> GetChainAsync(Hash id);
        Task<Chain> AddChainAsync(Hash chainId);
    }
}