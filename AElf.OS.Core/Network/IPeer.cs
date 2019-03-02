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
        
        Task AnnounceAsync(PeerNewBlockAnnouncement an);
        Task SendTransactionAsync(Transaction tx);
        //TODO: do not need height
        Task<Block> RequestBlockAsync(Hash hash, ulong height);
        Task<List<Hash>> GetBlockIdsAsync(Hash topHash, int count);
        
        Hash CurrentBlockHash { get; set; }
        
        ulong CurrentBlockHeight { get; set; }

        //TODO: help me implement it
        Task<List<Block>> GetBlocksAsync(Hash previousHash, int count);
    }
}