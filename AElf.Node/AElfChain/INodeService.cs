﻿using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Node.AElfChain
{
    // ReSharper disable InconsistentNaming
    public interface INodeService
    {
        void Initialize(int chainId, NodeConfiguration conf);
        bool Start(int chainId);
        bool Stop();
        Task<bool> CheckForkedAsync();

        Task<BlockHeaderList> GetBlockHeaderList(ulong index, int count);

        Task<Block> GetBlockFromHash(byte[] hash);
        Task<Block> GetBlockAtHeight(int height);
        Task<int> GetCurrentBlockHeightAsync();
    }
}