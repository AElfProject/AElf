using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeer
    {
        string PeerIpAddress { get; }
        string PubKey { get; }
        Hash CurrentBlockHash { get; set; }
        long CurrentBlockHeight { get; set; }

        Task SendDisconnectAsync();
        Task StopAsync();

        Task AnnounceAsync(PeerNewBlockAnnouncement an);
        Task SendTransactionAsync(Transaction tx);
        Task<Block> RequestBlockAsync(Hash hash);
        Task<List<Block>> GetBlocksAsync(Hash previousHash, int count);
    }
}