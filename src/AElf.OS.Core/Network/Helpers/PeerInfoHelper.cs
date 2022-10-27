using System.Linq;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Network.Types;

namespace AElf.OS.Network.Helpers;

public static class PeerInfoHelper
{
    public static PeerInfo FromNetworkPeer(IPeer peer)
    {
        return new PeerInfo
        {
            IpAddress = peer.RemoteEndpoint.ToString(),
            Pubkey = peer.Info.Pubkey,
            LastKnownLibHeight = peer.LastKnownLibHeight,
            ProtocolVersion = peer.Info.ProtocolVersion,
            ConnectionTime = peer.Info.ConnectionTime.Seconds,
            ConnectionStatus = peer.ConnectionStatus,
            Inbound = peer.Info.IsInbound,
            SyncState = peer.SyncState,
            BufferedAnnouncementsCount = peer.BufferedAnnouncementsCount,
            BufferedBlocksCount = peer.BufferedBlocksCount,
            BufferedTransactionsCount = peer.BufferedTransactionsCount,
            RequestMetrics = peer.GetRequestMetrics()?.Values.SelectMany(kvp => kvp).ToList(),
            NodeVersion = peer.Info.NodeVersion
        };
    }
}