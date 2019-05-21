using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;

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
        Task<BlockWithTransactions> RequestBlockAsync(Hash hash);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousHash, int count);
    }
}