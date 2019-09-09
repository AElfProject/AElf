using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Metrics;
using AElf.OS.Network.Protocol.Types;
using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeer
    {
        bool IsReady { get; }

        Hash LastKnownLibHash { get; }
        long LastKnownLibHeight { get; }
        IPEndPoint RemoteEndpoint { get; }
        string IpAddress { get; }
        
        int BufferedTransactionsCount { get; }
        int BufferedBlocksCount { get; }
        int BufferedAnnouncementsCount { get; }

        PeerConnectionInfo Info { get; }

        IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        
        void AddKnowBlock(BlockAnnouncement blockAnnouncement);
        void UpdateLastKnownLib(LibAnnouncement libAnnouncement);

        void EnqueueAnnouncement(BlockAnnouncement transaction, Action<NetworkException> sendCallback);
        void EnqueueTransaction(Transaction transaction, Action<NetworkException> sendCallback);
        void EnqueueBlock(BlockWithTransactions blockWithTransactions, Action<NetworkException> sendCallback);
        void EnqueueLibAnnouncement(LibAnnouncement libAnnouncement,Action<NetworkException> sendCallback);

        Task<Handshake> DoHandshakeAsync(Handshake handshake);
        Task<BlockWithTransactions> GetBlockByHashAsync(Hash hash);
        Task<List<BlockWithTransactions>> GetBlocksAsync(Hash previousHash, int count);
        Task<NodeList> GetNodesAsync(int count = NetworkConstants.DefaultDiscoveryMaxNodesToRequest);
        
        Task<bool> TryRecoverAsync();
        
        Dictionary<string, List<RequestMetric>> GetRequestMetrics();

        Task DisconnectAsync(bool gracefulDisconnect);
    }
}