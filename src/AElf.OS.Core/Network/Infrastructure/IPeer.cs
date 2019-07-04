using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeer
    {
        bool IsBest { get; set; }
        bool IsConnected { get; set; }
        bool IsReady { get; }
        Hash CurrentBlockHash { get; }
        long CurrentBlockHeight { get; }
        long LastKnowLibHeight { get; }
        
        string PeerIpAddress { get; }
        string PubKey { get; }
        int ProtocolVersion { get; }
        long ConnectionTime { get; }
        bool Inbound { get; }
        long StartHeight { get; }
        
        IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        
        Task DisconnectAsync();

        Dictionary<string, List<RequestMetric>> GetRequestMetrics();

        void HandlerRemoteAnnounce(PeerNewBlockAnnouncement peerNewBlockAnnouncement);

        Task<bool> TryWaitForStateChangedAsync();
        
        Task UpdateHandshakeAsync();
        Task<FinalizeConnectReply> FinalizeConnectAsync(Handshake handshake);
        Task SendDisconnectAsync();

        Task SendBlockAsync(BlockWithTransactions blockWithTransactions);
        Task AnnounceAsync(PeerNewBlockAnnouncement an);
        Task SendTransactionAsync(Transaction transaction);
        Task<BlockWithTransactions> RequestBlockAsync(Hash hash);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousHash, int count);
    }
}