using System.Linq;
using AElf.OS.Network.Domain;
using AElf.OS.Network.Infrastructure;

namespace AElf.OS.Network.Helpers
{
    public static class PeerHelper
    {
        public static Peer FromNetworkPeer(IPeer peer)
        {
            return new Peer
            {
                IpAddress = peer.RemoteEndpoint.ToString(),
                Pubkey = peer.Info.Pubkey,
                LastKnownLibHeight = peer.LastKnownLibHeight,
                ProtocolVersion = peer.Info.ProtocolVersion,
                ConnectionTime = peer.Info.ConnectionTime,
                Inbound = peer.Info.IsInbound,
                BufferedAnnouncementsCount = peer.BufferedAnnouncementsCount,
                BufferedBlocksCount = peer.BufferedBlocksCount,
                BufferedTransactionsCount = peer.BufferedTransactionsCount,
                RequestMetrics = peer.GetRequestMetrics()?.Values.SelectMany(kvp => kvp).ToList()
            };
        }
    }
}