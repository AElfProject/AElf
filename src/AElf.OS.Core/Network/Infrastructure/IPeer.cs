using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Metrics;
using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeer
    {
        bool IsBest { get; set; }
        bool IsReady { get; }
        
        long LastKnownLibHeight { get; }
        string IpAddress { get; }

        PeerInfo Info { get; }

        IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        void AddKnowBlock(BlockAnnouncement blockAnnouncement);

        Task<Handshake> DoHandshakeAsync(Handshake handshake);
        Task SendAnnouncementAsync(BlockAnnouncement an);
        Task SendTransactionAsync(Transaction transaction);
        Task SendBlockAsync(BlockWithTransactions blockWithTransactions);
        Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousHash, int count);
        Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest);
        
        Task<bool> TryRecoverAsync();
        
        Dictionary<string, List<RequestMetric>> GetRequestMetrics();

        Task DisconnectAsync(bool gracefulDisconnect);
    }
}