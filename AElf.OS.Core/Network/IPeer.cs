using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.OS.Network
{
    public interface IPeer
    {
        string PeerAddress { get; }
        
        Task SendDisconnectAsync();
        Task StopAsync();

        //TODO: change announce(NewBlockAnnouncement), create a new protobuf type in os
        Task AnnounceAsync(BlockHeader header);
        Task SendTransactionAsync(Transaction tx);
        //TODO: do not need height
        Task<Block> RequestBlockAsync(Hash hash, ulong height);
        Task<List<Hash>> GetBlockIdsAsync(Hash topHash, int count);
        
        Hash CurrentBlockHash { get; set; }
        
        ulong CurrentBlockHeight { get; set; }

        //TODO: help me implement it
        Task<List<Block>> GetBlocks(Hash previousHash, ulong height, int count);
    }
}