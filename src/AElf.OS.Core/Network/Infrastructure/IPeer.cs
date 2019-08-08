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
        bool IsConnected { get; set; }
        long LastKnownLibHeight { get; }
        string IpAddress { get; }

        PeerInfo Info { get; }

        IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        void AddKnowBlock(BlockAnnouncement blockAnnouncement);
        Task SendAnnouncementAsync(BlockAnnouncement an);
        Task SendTransactionAsync(Transaction transaction);
        Task SendBlockAsync(BlockWithTransactions blockWithTransactions);
        Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousHash, int count);
        Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest);
        
        Task<bool> TryRecoverAsync();
        
        Dictionary<string, List<RequestMetric>> GetRequestMetrics();
        Task SendConfirmHandshakeAsync();
        Task DisconnectAsync(bool gracefulDisconnect);
    }
}