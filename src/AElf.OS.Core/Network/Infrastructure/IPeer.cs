using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Types;
using AElf.OS.Network.Grpc;
using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeer
    {
        bool IsBest { get; set; }
        bool IsConnected { get; set; }
        bool IsReady { get; }
        
        long LastKnownLibHeight { get; }
        string IpAddress { get; }

        PeerInfo Info { get; }

        IReadOnlyDictionary<long, AcceptedBlockInfo> RecentBlockHeightAndHashMappings { get; }
        IReadOnlyDictionary<long, PreLibBlockInfo> PreLibBlockHeightAndHashMappings { get; }

        Task UpdateHandshakeAsync();
        Task SendAnnouncementAsync(BlockAnnouncement an);
        Task SendPreLibAnnounceAsync(PreLibAnnouncement preLibAnnouncement);
        Task SendPreLibConfirmAnnounceAsync(PreLibConfirmAnnouncement preLibConfirmAnnouncement);
        Task SendTransactionAsync(Transaction transaction);
        Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash);

        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousHash, int count);
        Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest);

        void ProcessReceivedAnnouncement(BlockAnnouncement blockAnnouncement);
        
        void ProcessReceivedPreLibAnnounce(PreLibAnnouncement preLibAnnouncement);

        bool HasBlock(long blockHeight, Hash blockHash);

        bool HasPreLib(long blockHeight, Hash blockHash);

        Task<bool> TryRecoverAsync();
        Dictionary<string, List<RequestMetric>> GetRequestMetrics();

        Task DisconnectAsync(bool gracefulDisconnect);
    }
}