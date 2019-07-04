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

        string IpAddress { get; }
        string PubKey { get; }
        int ProtocolVersion { get; }
        long ConnectionTime { get; }
        bool Inbound { get; }
        long StartHeight { get; }
        
        IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }

        Task UpdateHandshakeAsync();
        Task SendAnnouncementAsync(BlockAnnouncement an);
        Task SendTransactionAsync(Transaction transaction);
        Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousHash, int count);

        void ProcessReceivedAnnouncement(BlockAnnouncement blockAnnouncement);

        Task<bool> TryRecoverAsync();
        Dictionary<string, List<RequestMetric>> GetRequestMetrics();

        Task DisconnectAsync(bool gracefulDisconnect);
    }
}