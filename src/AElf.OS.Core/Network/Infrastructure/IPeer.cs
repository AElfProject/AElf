using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeer
    {
        string PeerIpAddress { get; }
        string PubKey { get; }
        Hash CurrentBlockHash { get; }
        long CurrentBlockHeight { get; }
        int ProtocolVersion { get; set; }
        long ConnectionTime { get; set; }
        bool Inbound { get; set; }
        long StartHeight { get; set; }
        IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }

        void HandlerRemoteAnnounce(PeerNewBlockAnnouncement peerNewBlockAnnouncement);

        Task SendDisconnectAsync();
        Task StopAsync();

        Task AnnounceAsync(PeerNewBlockAnnouncement an);
        Task SendTransactionAsync(Transaction tx);
        Task<BlockWithTransaction> RequestBlockAsync(Hash hash);
        Task<List<Block>> GetBlocksAsync(Hash previousHash, int count);
    }
}