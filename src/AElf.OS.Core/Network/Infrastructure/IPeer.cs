using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
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
        IPEndPoint RemoteEndpoint { get; }
        string IpAddress { get; }
        
        int BufferedTransactionsCount { get; }
        int BufferedBlocksCount { get; }
        int BufferedAnnouncementsCount { get; }

        PeerInfo Info { get; }

        IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        
        void AddKnowBlock(BlockAnnouncement blockAnnouncement);

        void EnqueueAnnouncement(BlockAnnouncement transaction, Action<NetworkException> sendCallback);
        void EnqueueTransaction(Transaction transaction, Action<NetworkException> sendCallback);
        void EnqueueBlock(BlockWithTransactions blockWithTransactions, Action<NetworkException> sendCallback);

        Task<Handshake> DoHandshakeAsync(Handshake handshake);
        Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousHash, int count);
        Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest);
        
        Task<bool> TryRecoverAsync();
        
        Dictionary<string, List<RequestMetric>> GetRequestMetrics();

        Task DisconnectAsync(bool gracefulDisconnect);
    }
}