using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
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

        IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        
        void EnqueueAnnouncement(BlockAnnouncement transaction, Action<NetworkException> errorCallback);
        void EnqueueTransaction(Transaction transaction, Action<NetworkException> errorCallback);
        void EnqueueBlock(BlockWithTransactions blockWithTransactions, Action<NetworkException> errorCallback);

        Task UpdateHandshakeAsync();
        Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousHash, int count);
        Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest);

        void ProcessReceivedAnnouncement(BlockAnnouncement blockAnnouncement);

        Task<bool> TryRecoverAsync();
        Dictionary<string, List<RequestMetric>> GetRequestMetrics();

        Task DisconnectAsync(bool gracefulDisconnect);
    }
}